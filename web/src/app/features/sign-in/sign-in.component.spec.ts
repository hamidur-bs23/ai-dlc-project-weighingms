import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { SessionService } from '@core/auth/session.service';
import { FunctionKeysService } from '@core/keyboard/function-keys.service';
import { operatorSession } from '../../../testing/fixtures';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
} from '../../../testing/test-providers';
import { SignInComponent } from './sign-in.component';

describe('SignInComponent', () => {
  let navigate: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    document.cookie = 'XSRF-TOKEN=test-token';
    await render(SignInComponent, {
      providers: [provideTestHttp(), provideRouter([]), provideTestTransloco()],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true) as never;
  });

  afterEach(() => {
    httpMock().verify();
  });

  it('should open the batch queue for a known badge', async () => {
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Badge number'), ' OP-1001 ');
    await user.type(screen.getByLabelText('Shift (optional)'), 'A');
    await user.click(screen.getByRole('button', { name: /OK/ }));

    const req = httpMock().expectOne('/api/session');
    expect(req.request.body).toEqual({ badgeId: 'OP-1001', shift: 'A' });
    req.flush(operatorSession);

    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/batches']);
    });
    expect(TestBed.inject(SessionService).session()?.displayName).toBe('R. Karim');
  });

  it('should show a translated error with Ref for an unknown badge, and stay', async () => {
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Badge number'), 'OP-9999');
    await user.click(screen.getByRole('button', { name: /OK/ }));

    httpMock()
      .expectOne('/api/session')
      .flush(problem('Auth.BadgeNotRecognised'), { status: 401, statusText: 'Unauthorized' });

    const alert = await screen.findByRole('alert');
    expect(alert.textContent).toContain('Badge not recognised.');
    expect(alert.textContent).toContain('Ref 4bf92f35');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('should ask for a badge instead of calling the Api with an empty one', async () => {
    await userEvent.setup().click(screen.getByRole('button', { name: /OK/ }));
    expect(await screen.findByText('Enter your badge number.')).toBeTruthy();
    expect(screen.getByLabelText('Badge number').getAttribute('aria-invalid')).toBe('true');
    httpMock().expectNone('/api/session');
  });

  it('should sign in with F1, the key labelled on OK', async () => {
    await userEvent.setup().type(screen.getByLabelText('Badge number'), 'OP-1001');
    TestBed.inject(FunctionKeysService).handle(
      new KeyboardEvent('keydown', { key: 'F1', cancelable: true }),
    );
    httpMock().expectOne('/api/session').flush(operatorSession);
    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/batches']);
    });
  });
});
