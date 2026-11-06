import { render, screen } from '@testing-library/angular';
import type { Ticket } from '@core/api/api.types';
import { provideTestTransloco } from '../../../testing/test-providers';
import { TicketPreviewComponent } from './ticket-preview.component';

const ticket: Ticket = {
  batchId: 'B-24187',
  state: 'printPending',
  printCount: 0,
  lines: ['       FCI DYEING - WEIGHING TICKET', 'BATCH    B-24187', '  1240.0 g      1241.5 g'],
  barcode: { symbology: 'Code128', value: 'B-24187' },
  firstPrintedAt: null,
  lastPrintedAt: null,
};

const open = (inputs: { ticket: Ticket; showReprintNote?: boolean }) =>
  render(TicketPreviewComponent, { providers: [provideTestTransloco()], inputs });

/** Where each bar sits and how wide it is, which is what a scanner reads. */
const bars = () =>
  [...screen.getByRole('img').querySelectorAll('rect')]
    .map((bar) => `${bar.getAttribute('x')}:${bar.getAttribute('width')}`)
    .join(',');

describe('TicketPreviewComponent', () => {
  it('should show the server lines exactly as sent, spacing included', async () => {
    await open({ ticket });

    expect(screen.getByTestId('ticket-lines').textContent).toBe(ticket.lines.join('\n'));
  });

  it('should draw the barcode and print its value beneath it', async () => {
    await open({ ticket });

    const barcode = screen.getByRole('img', { name: 'Code 128 barcode of B-24187' });
    expect(barcode.querySelectorAll('rect').length).toBeGreaterThan(10);
    expect(screen.getByTestId('ticket-barcode-value').textContent).toBe('B-24187');
  });

  it('should draw different bars for a different batch', async () => {
    const { rerender } = await open({ ticket });
    const first = bars();

    await rerender({
      inputs: { ticket: { ...ticket, barcode: { symbology: 'Code128', value: 'B-24188' } } },
      partialUpdate: true,
    });

    await vi.waitFor(() => {
      expect(bars()).not.toBe(first);
    });
  });

  it('should draw no bars for a symbology it does not know, and still show the value', async () => {
    await open({ ticket: { ...ticket, barcode: { symbology: 'QR', value: 'B-24187' } } });

    expect(screen.queryByRole('img')).toBeNull();
    expect(screen.getByTestId('ticket-barcode-value').textContent).toBe('B-24187');
  });

  it('should show print pending', async () => {
    await open({ ticket });

    const state = screen.getByTestId('ticket-preview-state').textContent;
    expect(state).toContain('Print pending');
    expect(state).not.toContain('REPRINT');
  });

  it('should show the count and say the next copy is marked REPRINT once printed', async () => {
    await open({ ticket: { ...ticket, state: 'printed', printCount: 2 } });

    const state = screen.getByTestId('ticket-preview-state').textContent;
    expect(state).toContain('Printed (2)');
    expect(state).toContain('The next copy will be marked REPRINT.');
  });

  it('should leave out the REPRINT note where no one can print', async () => {
    await open({
      ticket: { ...ticket, state: 'printed', printCount: 1 },
      showReprintNote: false,
    });

    expect(screen.getByTestId('ticket-preview-state').textContent).not.toContain('REPRINT');
  });
});
