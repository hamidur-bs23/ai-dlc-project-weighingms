import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { AppError } from '@core/errors/app-error';
import { provideTestTransloco } from '../../../../testing/test-providers';
import { BannerComponent } from './banner.component';

const trace = '00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01';

describe('BannerComponent', () => {
  it('should show the translated message for the code and the Ref, never raw detail', async () => {
    await render(BannerComponent, {
      providers: [provideTestTransloco()],
      inputs: { error: new AppError('Scale.Unavailable', 503, trace) },
    });
    expect(screen.getByRole('alert').textContent.replace(/\s+/g, ' ').trim()).toBe(
      '! The scale is not responding. Ref 4bf92f35',
    );
  });

  it('should fall back to the generic message for a code without a translation', async () => {
    await render(BannerComponent, {
      providers: [provideTestTransloco()],
      inputs: { error: new AppError('Printer.OutOfPaper', 503, null) },
    });
    expect(screen.getByRole('alert').textContent).toContain('Something went wrong.');
  });

  it('should emit retry when Retry is touched', async () => {
    const retry = vi.fn();
    await render(BannerComponent, {
      providers: [provideTestTransloco()],
      inputs: { error: new AppError('OrgaTex.Unavailable', 503, null), retryable: true },
      on: { retry },
    });
    await userEvent.setup().click(screen.getByRole('button', { name: 'Retry' }));
    expect(retry).toHaveBeenCalledOnce();
  });

  it('should show an info message as a status, not an alert', async () => {
    await render(BannerComponent, {
      providers: [provideTestTransloco()],
      inputs: { kind: 'info', messageKey: 'batchQueue.empty' },
    });
    expect(screen.getByRole('status').textContent).toContain('No batches released for weighing.');
    expect(screen.queryByRole('alert')).toBeNull();
  });
});
