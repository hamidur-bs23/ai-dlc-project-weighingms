import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import type { BatchSummary, Ticket } from '@core/api/api.types';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { FunctionKeysService } from '@core/keyboard/function-keys.service';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
  settle,
} from '../../../testing/test-providers';
import { BatchSummaryComponent, SUMMARY_REFRESH_MS } from './batch-summary.component';

const SUMMARY_URL = '/api/batches/B-24187/session/summary';
const TICKET_URL = '/api/batches/B-24187/session/ticket';
const PRINT_URL = '/api/batches/B-24187/session/ticket/print';
const RETRY_URL = '/api/batches/B-24187/session/write-back/retry';

const summary: BatchSummary = {
  batchId: 'B-24187',
  orderId: 'SO-9931',
  machine: 'JET-04',
  shade: 'Teal 4C',
  status: 'completed',
  done: 1,
  total: 1,
  allWithinTolerance: true,
  overrideCount: 0,
  totalDispensed: { value: 1.24, unit: 'kg' },
  startedAt: '2026-11-05T01:41:41Z',
  completedAt: '2026-11-05T01:48:22Z',
  elapsedSeconds: 401,
  operators: ['R. Karim'],
  orgatex: {
    state: 'sent',
    writtenAt: '2026-11-05T01:48:25Z',
    attempts: 1,
    nextAttemptAt: null,
    errorCode: null,
  },
  ticket: { state: 'printPending', printCount: 0 },
  lines: [
    {
      sequence: 1,
      chemicalId: 'DYE-1180',
      chemicalName: 'Remazol Brill. Blue RSPL',
      required: { value: 1240, unit: 'g' },
      actual: { value: 1240, unit: 'g' },
      status: 'targetAchieved',
      override: false,
    },
  ],
};

const pending: Ticket = {
  batchId: 'B-24187',
  state: 'printPending',
  printCount: 0,
  lines: ['BATCH    B-24187'],
  barcode: { symbology: 'Code128', value: 'B-24187' },
  firstPrintedAt: null,
  lastPrintedAt: null,
};
const printed = (printCount: number): Ticket => ({ ...pending, state: 'printed', printCount });
const printedSummary = (printCount: number): BatchSummary => ({
  ...summary,
  ticket: { state: 'printed', printCount },
});

const open = (readOnly = false) =>
  render(BatchSummaryComponent, {
    providers: [provideTestHttp(), provideRouter([]), provideTestTransloco()],
    inputs: { batchId: 'B-24187', readOnly },
  });

const press = (key: string) =>
  TestBed.inject(FunctionKeysService).handle(
    new KeyboardEvent('keydown', { key, cancelable: true }),
  );

/** Opens the screen with a summary and ticket loaded. */
const openLoaded = async (ticket = pending, loaded = summary, readOnly = false) => {
  await open(readOnly);
  httpMock().expectOne(SUMMARY_URL).flush(loaded);
  await screen.findByTestId('ticket-state');
  httpMock().expectOne(TICKET_URL).flush(ticket);
  await screen.findByTestId('ticket-preview');
};

const button = (name: RegExp) => screen.findByRole('button', { name });
const ticketRow = () => screen.getByTestId('ticket-state').textContent.trim();

describe('Confirm & print (screen 3)', () => {
  afterEach(() => {
    httpMock().verify();
    vi.useRealTimers();
  });

  describe('the screen', () => {
    it('should show the preview beside the summary', async () => {
      await openLoaded();

      expect(screen.getByTestId('ticket-lines').textContent).toBe('BATCH    B-24187');
      expect(screen.getByTestId('ticket-preview-state').textContent).toContain('Print pending');
      expect(ticketRow()).toBe('Print pending');
    });

    it('should read the ticket once while it follows a pending write-back', async () => {
      vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
      const following: BatchSummary = {
        ...summary,
        orgatex: { ...summary.orgatex, state: 'pending', writtenAt: null },
      };
      await open();
      httpMock().expectOne(SUMMARY_URL).flush(following);
      await settle();
      httpMock().expectOne(TICKET_URL).flush(pending);
      await settle();

      await vi.advanceTimersByTimeAsync(SUMMARY_REFRESH_MS);
      httpMock().expectOne(SUMMARY_URL).flush(summary);
      await settle();

      httpMock().expectNone(TICKET_URL);
    });

    it('should show no preview and no Print key for a batch without a ticket', async () => {
      await open();
      httpMock()
        .expectOne(SUMMARY_URL)
        .flush({ ...summary, ticket: { state: 'notAvailable', printCount: 0 } });

      await button(/Next batch/);
      expect(screen.queryByTestId('ticket-preview')).toBeNull();
      expect(screen.queryByRole('button', { name: /Print ticket/ })).toBeNull();
      httpMock().expectNone(TICKET_URL);
    });

    it('should keep the summary and offer another try when the ticket cannot be read', async () => {
      await open();
      httpMock().expectOne(SUMMARY_URL).flush(summary);
      await screen.findByTestId('ticket-state');
      httpMock()
        .expectOne(TICKET_URL)
        .flush(problem('Unexpected'), { status: 500, statusText: 'Error' });

      await userEvent.setup().click(await button(/Retry/));
      httpMock().expectOne(TICKET_URL).flush(pending);

      expect(await screen.findByTestId('ticket-preview')).toBeTruthy();
      expect(screen.getByTestId('summary-weighed')).toBeTruthy();
    });

    it.each([
      ['sent', true],
      ['pending', false],
      ['syncFailed', false],
    ])('with OrgaTEX %s the consumption note shows: %s', async (state, shown) => {
      vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
      await open();
      httpMock()
        .expectOne(SUMMARY_URL)
        .flush({
          ...summary,
          ticket: { state: 'notAvailable', printCount: 0 },
          orgatex: { ...summary.orgatex, state },
        });
      await settle();
      await screen.findByTestId('orgatex-state');

      expect(screen.queryByTestId('consumption-note') !== null).toBe(shown);
    });

    it('should not claim the container was released', async () => {
      await openLoaded();

      expect(screen.getByTestId('consumption-note').textContent.trim()).toBe(
        'Actual consumption posted against the batch.',
      );
    });
  });

  describe('printing', () => {
    it('should send the count on screen, then show Printed and offer Reprint', async () => {
      await openLoaded();

      await userEvent.setup().click(await button(/Print ticket/));
      const request = httpMock().expectOne(PRINT_URL);
      expect(request.request.body).toEqual({ expectedPrintCount: 0 });
      request.flush(printed(1));

      expect(await button(/Reprint ticket/)).toBeTruthy();
      expect(ticketRow()).toBe('Printed (1)');
      expect(screen.getByTestId('ticket-preview-state').textContent).toContain('REPRINT');
    });

    it('should print with F1', async () => {
      await openLoaded();

      expect(press('F1')).toBe(true);

      httpMock().expectOne(PRINT_URL).flush(printed(1));
    });

    it('should send the new count on a reprint', async () => {
      await openLoaded(printed(1), printedSummary(1));

      press('F1');

      const request = httpMock().expectOne(PRINT_URL);
      expect(request.request.body).toEqual({ expectedPrintCount: 1 });
      request.flush(printed(2));
      expect(await screen.findByText('Printed (2)')).toBeTruthy();
    });

    it('should send one press while one is in flight', async () => {
      await openLoaded();

      press('F1');
      expect(press('F1')).toBe(false);

      httpMock().expectOne(PRINT_URL).flush(printed(1));
    });

    it('should disable Print while a press is in flight', async () => {
      await openLoaded();
      const print = await button(/Print ticket/);

      press('F1');
      await vi.waitFor(() => {
        expect((print as HTMLButtonElement).disabled).toBe(true);
      });

      httpMock().expectOne(PRINT_URL).flush(printed(1));
    });

    it('should not print with F1 before the ticket has loaded', async () => {
      await open();
      httpMock().expectOne(SUMMARY_URL).flush(summary);
      await screen.findByTestId('ticket-state');

      expect(press('F1')).toBe(false);

      httpMock().expectOne(TICKET_URL).flush(pending);
      httpMock().expectNone(PRINT_URL);
    });

    it('should keep the screen and the ticket pending when the printer is down', async () => {
      await openLoaded();

      press('F1');
      httpMock()
        .expectOne(PRINT_URL)
        .flush(problem('Printer.Unavailable'), { status: 503, statusText: 'Unavailable' });

      expect(await screen.findByRole('alert')).toBeTruthy();
      expect(screen.getByRole('alert').textContent).toContain('The printer is not responding');
      expect(screen.getByTestId('ticket-preview')).toBeTruthy();
      expect(screen.getByTestId('summary-weighed')).toBeTruthy();
      expect(ticketRow()).toBe('Print pending');
    });

    it('should clear the printer error and print once the operator presses again', async () => {
      await openLoaded();
      press('F1');
      httpMock()
        .expectOne(PRINT_URL)
        .flush(problem('Printer.Unavailable'), { status: 503, statusText: 'Unavailable' });
      await screen.findByRole('alert');
      await vi.waitFor(() => {
        expect(press('F1')).toBe(true);
      });

      const request = httpMock().expectOne(PRINT_URL);
      expect(request.request.body).toEqual({ expectedPrintCount: 0 });
      request.flush(printed(1));

      await screen.findByText('Printed (1)');
      expect(screen.queryByRole('alert')).toBeNull();
    });

    it('should catch up without printing when another press got there first', async () => {
      await openLoaded();

      press('F1');
      httpMock()
        .expectOne(PRINT_URL)
        .flush(problem('Ticket.AlreadyPrinted'), { status: 409, statusText: 'Conflict' });

      await vi.waitFor(() => {
        httpMock().expectOne(TICKET_URL).flush(printed(1));
      });

      expect(await button(/Reprint ticket/)).toBeTruthy();
      expect(ticketRow()).toBe('Printed (1)');
      expect(screen.getByRole('alert').textContent).toContain('already been printed');
      httpMock().expectNone(PRINT_URL);
    });

    it('should not let an older summary refresh undo a print', async () => {
      vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
      const following: BatchSummary = {
        ...summary,
        orgatex: { ...summary.orgatex, state: 'pending', writtenAt: null },
      };
      await open();
      httpMock().expectOne(SUMMARY_URL).flush(following);
      await settle();
      httpMock().expectOne(TICKET_URL).flush(pending);
      await settle();

      press('F1');
      await vi.advanceTimersByTimeAsync(SUMMARY_REFRESH_MS);
      const refresh = httpMock().expectOne(SUMMARY_URL);
      httpMock().expectOne(PRINT_URL).flush(printed(1));
      await settle();
      refresh.flush(summary); // read before the print landed: it still says print pending
      await settle();
      await vi.advanceTimersByTimeAsync(0); // zoneless rendering is scheduled on the faked timers

      expect(ticketRow()).toBe('Printed (1)');
    });
  });

  describe('Next batch', () => {
    it('should open the queue with F3, even with the ticket unprinted', async () => {
      await openLoaded();
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      expect(press('F3')).toBe(true);

      await vi.waitFor(() => {
        expect(navigate).toHaveBeenCalledWith(['/batches']);
      });
      httpMock().expectNone(PRINT_URL);
    });
  });

  describe('read-only, from the review list', () => {
    it('should show the summary and preview with only Back', async () => {
      await openLoaded(printed(1), printedSummary(1), true);

      expect(screen.getByTestId('read-only').textContent.trim()).toBe('Review · read-only');
      expect(screen.getByTestId('ticket-lines')).toBeTruthy();
      expect(screen.getAllByRole('button').map((b) => b.textContent.trim())).toEqual([
        'Back to completed batches',
      ]);
      expect(screen.getByTestId('ticket-preview-state').textContent).not.toContain('REPRINT');
    });

    it('should ignore F1 and F3', async () => {
      await openLoaded(printed(1), printedSummary(1), true);
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      expect(press('F1')).toBe(false);
      expect(press('F3')).toBe(false);

      httpMock().expectNone(PRINT_URL);
      expect(navigate).not.toHaveBeenCalled();
    });

    it('should show sync failed without Retry', async () => {
      await open(true);
      httpMock()
        .expectOne(SUMMARY_URL)
        .flush({
          ...summary,
          ticket: { state: 'notAvailable', printCount: 0 },
          orgatex: { ...summary.orgatex, state: 'syncFailed', writtenAt: null },
        });

      expect((await screen.findByTestId('orgatex-state')).textContent).toContain('Sync failed');
      expect(screen.queryByRole('button', { name: 'Retry' })).toBeNull();
      httpMock().expectNone(RETRY_URL);
    });

    it('should go back to the list', async () => {
      await openLoaded(printed(1), printedSummary(1), true);
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      await userEvent.setup().click(await button(/Back to completed batches/));

      expect(navigate).toHaveBeenCalledWith(['/review']);
    });
  });

  describe('the header', () => {
    it('should name the batch in the header when the screen is opened by URL', async () => {
      await openLoaded();

      expect(TestBed.inject(ActiveBatchService).batch()).toEqual(
        expect.objectContaining({ batchId: 'B-24187', orderId: 'SO-9931', machine: 'JET-04' }),
      );
    });

    it('should name the batch from the review list, and clear it on Back', async () => {
      await openLoaded(printed(1), printedSummary(1), true);
      expect(TestBed.inject(ActiveBatchService).batch()?.batchId).toBe('B-24187');
      vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

      await userEvent.setup().click(await button(/Back to completed batches/));

      expect(TestBed.inject(ActiveBatchService).batch()).toBeNull();
    });
  });
});
