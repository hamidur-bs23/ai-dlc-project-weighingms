import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import type { BatchSummary } from '@core/api/api.types';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
} from '../../../testing/test-providers';
import { BatchSummaryComponent, SUMMARY_REFRESH_MS } from './batch-summary.component';

const summary: BatchSummary = {
  batchId: 'B-24187',
  orderId: 'SO-9931',
  machine: 'JET-04',
  shade: 'Teal 4C',
  status: 'completed',
  done: 5,
  total: 5,
  allWithinTolerance: false,
  overrideCount: 1,
  totalDispensed: { value: 35.322, unit: 'kg' },
  startedAt: '2026-11-05T01:41:41Z',
  completedAt: '2026-11-05T01:48:22Z',
  elapsedSeconds: 401,
  operators: ['R. Karim', 'S. Akter'],
  orgatex: {
    state: 'notAvailable',
    writtenAt: null,
    attempts: 0,
    nextAttemptAt: null,
    errorCode: null,
  },
  ticket: { state: 'notAvailable', printCount: 0 },
  lines: [
    {
      sequence: 1,
      chemicalId: 'DYE-1180',
      chemicalName: 'Remazol Brill. Blue RSPL',
      required: { value: 1240, unit: 'g' },
      actual: { value: 1241.5, unit: 'g' },
      status: 'withinRange',
      override: false,
    },
    {
      sequence: 2,
      chemicalId: 'DYE-2204',
      chemicalName: 'Remazol Red RB',
      required: { value: 386, unit: 'g' },
      actual: { value: 389.1, unit: 'g' },
      status: 'overweight',
      override: true,
    },
  ],
};

describe('BatchSummaryComponent (summary)', () => {
  beforeEach(async () => {
    await render(BatchSummaryComponent, {
      providers: [provideTestHttp(), provideRouter([]), provideTestTransloco()],
      inputs: { batchId: 'B-24187' },
    });
  });

  afterEach(() => {
    httpMock().verify();
  });

  it('should show the server summary read-only', async () => {
    httpMock().expectOne('/api/batches/B-24187/session/summary').flush(summary);

    expect(await screen.findByRole('heading', { name: 'Batch B-24187 weighed' })).toBeTruthy();
    expect(screen.getByTestId('summary-weighed').textContent.trim()).toBe('5 of 5');
    expect(screen.getByText('NO · 1 override(s)')).toBeTruthy();
    expect(screen.getByText('35.322 kg')).toBeTruthy();
    expect(screen.getByText('6 min 41 s')).toBeTruthy();
    expect(screen.getByText('R. Karim, S. Akter')).toBeTruthy();
    const rows = screen.getAllByRole('row').slice(1);
    expect(within(rows[1]).getByText('OVR')).toBeTruthy();
    expect(within(rows[1]).getByText('Overweight', { exact: false })).toBeTruthy();
    expect(screen.queryByRole('textbox')).toBeNull();
  });

  it('should offer Retry when the summary cannot be loaded', async () => {
    httpMock()
      .expectOne('/api/batches/B-24187/session/summary')
      .flush(problem('Weighing.SessionNotFound'), { status: 404, statusText: 'x' });

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Retry' }));
    httpMock().expectOne('/api/batches/B-24187/session/summary').flush(summary);
    expect(await screen.findByText('35.322 kg')).toBeTruthy();
  });

  it('should open the queue with Next batch', async () => {
    httpMock().expectOne('/api/batches/B-24187/session/summary').flush(summary);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    await userEvent.setup().click(await screen.findByRole('button', { name: /Next batch/ }));

    expect(navigate).toHaveBeenCalledWith(['/batches']);
  });
});

describe('BatchSummaryComponent (OrgaTEX write-back)', () => {
  const SUMMARY_URL = '/api/batches/B-24187/session/summary';
  const RETRY_URL = '/api/batches/B-24187/session/write-back/retry';

  const withOrgatex = (orgatex: Partial<BatchSummary['orgatex']>): BatchSummary => ({
    ...summary,
    orgatex: { ...summary.orgatex, ...orgatex },
  });
  const sent = withOrgatex({ state: 'sent', writtenAt: '2026-11-05T01:48:25Z', attempts: 1 });
  const pending = withOrgatex({ state: 'pending' });
  const failed = withOrgatex({
    state: 'syncFailed',
    attempts: 5,
    errorCode: 'OrgaTex.Unavailable',
  });

  const open = () =>
    render(BatchSummaryComponent, {
      providers: [provideTestHttp(), provideRouter([]), provideTestTransloco()],
      inputs: { batchId: 'B-24187' },
    });

  const orgatexText = async () =>
    (await screen.findByTestId('orgatex-state')).textContent.replace(/\s+/g, ' ').trim();

  afterEach(() => {
    httpMock().verify();
    vi.useRealTimers();
  });

  it('should show CONFIRMED with the plant time once written (STEP 26)', async () => {
    await open();
    httpMock().expectOne(SUMMARY_URL).flush(sent);

    expect(await orgatexText()).toMatch(/^CONFIRMED \d{2}:\d{2}:\d{2}$/);
    expect(screen.getByTestId('orgatex-state').classList.contains('good')).toBe(true);
    expect(screen.queryByRole('button', { name: 'Retry' })).toBeNull();
  });

  it.each([
    [{ state: 'notAvailable', printCount: 0 }, '—'],
    [{ state: 'printPending', printCount: 0 }, 'Print pending'],
    [{ state: 'printed', printCount: 2 }, 'Printed (2)'],
  ])('should show the ticket state %o', async (ticket, expected) => {
    await open();
    httpMock()
      .expectOne(SUMMARY_URL)
      .flush({ ...summary, ticket });

    expect((await screen.findByTestId('ticket-state')).textContent.trim()).toBe(expected);
    if (ticket.state !== 'notAvailable') {
      httpMock()
        .expectOne('/api/batches/B-24187/session/ticket')
        .flush({
          batchId: 'B-24187',
          ...ticket,
          lines: ['BATCH    B-24187'],
          barcode: { symbology: 'Code128', value: 'B-24187' },
          firstPrintedAt: null,
          lastPrintedAt: null,
        });
    }
  });

  it('should show nothing but a dash before the batch completes', async () => {
    await open();
    httpMock().expectOne(SUMMARY_URL).flush(summary);

    expect(await orgatexText()).toBe('—');
  });

  it('should show pending and follow it until OrgaTEX confirms', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    await open();
    httpMock().expectOne(SUMMARY_URL).flush(pending);
    await vi.advanceTimersByTimeAsync(0);
    expect(await orgatexText()).toBe('Pending sync');

    await vi.advanceTimersByTimeAsync(SUMMARY_REFRESH_MS - 1);
    httpMock().expectNone(SUMMARY_URL);
    await vi.advanceTimersByTimeAsync(1);
    httpMock().expectOne(SUMMARY_URL).flush(sent);
    await vi.advanceTimersByTimeAsync(0);
    expect(await orgatexText()).toMatch(/^CONFIRMED/);

    await vi.advanceTimersByTimeAsync(SUMMARY_REFRESH_MS * 3);
    httpMock().expectNone(SUMMARY_URL); // sent: polling has stopped
  });

  it('should stop following when the write-back fails', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    await open();
    httpMock().expectOne(SUMMARY_URL).flush(pending);
    await vi.advanceTimersByTimeAsync(SUMMARY_REFRESH_MS);
    httpMock().expectOne(SUMMARY_URL).flush(failed);
    await vi.advanceTimersByTimeAsync(SUMMARY_REFRESH_MS * 3);

    httpMock().expectNone(SUMMARY_URL);
  });

  it('should stop following when the screen closes', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    const { fixture } = await open();
    httpMock().expectOne(SUMMARY_URL).flush(pending);
    await vi.advanceTimersByTimeAsync(0);

    fixture.destroy();
    await vi.advanceTimersByTimeAsync(SUMMARY_REFRESH_MS * 3);

    httpMock().expectNone(SUMMARY_URL);
  });

  it('should not start following when the response arrives after the screen closed', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    const { fixture } = await open();
    const inFlight = httpMock().expectOne(SUMMARY_URL);

    fixture.destroy();
    inFlight.flush(pending);
    await vi.advanceTimersByTimeAsync(SUMMARY_REFRESH_MS * 3);

    httpMock().expectNone(SUMMARY_URL);
  });

  it('should show sync failed with Retry, then post it and follow it to confirmed', async () => {
    await open();
    httpMock().expectOne(SUMMARY_URL).flush(failed);
    expect(await orgatexText()).toBe('Sync failed Retry');
    expect(screen.getByTestId('orgatex-state').classList.contains('bad')).toBe(true);

    await userEvent.setup().click(screen.getByRole('button', { name: 'Retry' }));

    const retry = httpMock().expectOne(RETRY_URL);
    expect(retry.request.method).toBe('POST');
    retry.flush(sent.orgatex);
    await vi.waitFor(() => {
      httpMock().expectOne(SUMMARY_URL).flush(sent);
    });
    expect(await orgatexText()).toMatch(/^CONFIRMED/);
  });

  it('should show the error when the retry is refused and allow another try', async () => {
    await open();
    httpMock().expectOne(SUMMARY_URL).flush(failed);
    await screen.findByTestId('orgatex-state');

    await userEvent.setup().click(screen.getByRole('button', { name: 'Retry' }));
    httpMock()
      .expectOne(RETRY_URL)
      .flush(problem('WriteBack.NotFailed'), { status: 409, statusText: 'x' });

    expect(await screen.findByText('The write-back is not in a failed state.')).toBeTruthy();
    httpMock().expectNone(SUMMARY_URL);
  });
});
