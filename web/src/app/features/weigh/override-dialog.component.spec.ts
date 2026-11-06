import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { AppError } from '@core/errors/app-error';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
} from '../../../testing/test-providers';
import { OverrideDialogComponent } from './override-dialog.component';

describe('OverrideDialogComponent', () => {
  const override = vi.fn<(grantId: string, reason: string) => Promise<AppError | null>>();
  const close = vi.fn();

  beforeEach(async () => {
    override.mockReset();
    close.mockReset();
    await render(OverrideDialogComponent, {
      providers: [
        provideTestHttp(),
        provideTestTransloco(),
        { provide: DIALOG_DATA, useValue: { store: { override } } },
        { provide: DialogRef, useValue: { close } },
      ],
    });
  });

  afterEach(() => {
    httpMock().verify();
  });

  const fill = async (badge: string, pin: string, reason: string) => {
    const user = userEvent.setup();
    if (badge) await user.type(screen.getByLabelText('Supervisor badge'), badge);
    if (pin) await user.type(screen.getByLabelText('PIN'), pin);
    if (reason) await user.type(screen.getByLabelText('Reason (required)'), reason);
    await user.click(screen.getByRole('button', { name: 'Authorise override' }));
  };

  it('should require badge, PIN and a reason before anything is sent', async () => {
    await fill('SV-2001', '2468', '');

    expect(screen.getByText('Badge, PIN and a reason are required.')).toBeTruthy();
    httpMock().expectNone('/api/session/step-up');
    expect(override).not.toHaveBeenCalled();
  });

  it('should mask the PIN', () => {
    expect(screen.getByLabelText('PIN').getAttribute('type')).toBe('password');
  });

  it('should show a wrong PIN, clear it, and record nothing', async () => {
    await fill('SV-2001', '1111', 'Accepted');

    const stepUp = httpMock().expectOne('/api/session/step-up');
    expect(stepUp.request.body).toEqual({ badgeId: 'SV-2001', pin: '1111', action: 'override' });
    stepUp.flush(problem('Auth.StepUpFailed'), { status: 403, statusText: 'x' });

    expect((await screen.findByRole('alert')).textContent).toContain(
      'Supervisor badge or PIN not accepted.',
    );
    expect(screen.getByLabelText<HTMLInputElement>('PIN').value).toBe('');
    expect(override).not.toHaveBeenCalled();
    expect(close).not.toHaveBeenCalled();
  });

  it('should use the grant for the override and close on success', async () => {
    override.mockResolvedValue(null);

    await fill('SV-2001', '2468', '  Shade lab accepts  ');
    httpMock().expectOne('/api/session/step-up').flush({
      grantId: 'g-1',
      supervisorBadgeId: 'SV-2001',
      supervisorName: 'M. Rahman',
      action: 'override',
      expiresAt: 'x',
    });

    await vi.waitFor(() => {
      expect(close).toHaveBeenCalledWith(true);
    });
    expect(override).toHaveBeenCalledWith('g-1', 'Shade lab accepts');
  });

  it('should keep the dialog open and show a refused override (four eyes)', async () => {
    override.mockResolvedValue(new AppError('Auth.FourEyesRequired', 403, null));

    await fill('SV-2001', '2468', 'Self');
    httpMock().expectOne('/api/session/step-up').flush({
      grantId: 'g-2',
      supervisorBadgeId: 'SV-2001',
      supervisorName: 'M. Rahman',
      action: 'override',
      expiresAt: 'x',
    });

    expect((await screen.findByRole('alert')).textContent).toContain(
      'Another supervisor must authorise this override.',
    );
    expect(close).not.toHaveBeenCalled();
  });

  it('should cancel', async () => {
    await userEvent.setup().click(screen.getByRole('button', { name: 'Cancel' }));
    expect(close).toHaveBeenCalled();
  });
});
