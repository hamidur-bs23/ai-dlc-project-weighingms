import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { SessionService } from '@core/auth/session.service';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { b24187, operatorSession } from '../../../testing/fixtures';
import { httpMock, provideTestHttp, provideTestTransloco } from '../../../testing/test-providers';
import { TopBarComponent } from './top-bar.component';

describe('TopBarComponent', () => {
  beforeEach(async () => {
    await render(TopBarComponent, {
      providers: [provideTestHttp(), provideRouter([]), provideTestTransloco()],
    });
    const restore = TestBed.inject(SessionService).restore();
    httpMock().expectOne('/api/session').flush(operatorSession);
    await restore;
    await screen.findByRole('button', { name: /Operator R. Karim/ });
  });

  afterEach(() => {
    httpMock().verify();
  });

  it('should show WeighingMS, the station and the operator chip with name and shift', () => {
    expect(screen.getByText('WeighingMS')).toBeTruthy();
    expect(screen.getByText('STATION 01')).toBeTruthy();
    const chip = screen.getByRole('button', { name: 'Operator R. Karim: show actions' });
    expect(chip.textContent.replace(/\s+/g, ' ')).toContain('R. Karim · Shift A');
  });

  it('should show the BATCH · ORDER · MACHINE crumb only while a batch is open', async () => {
    expect(screen.queryByTestId('batch-crumb')).toBeNull();
    TestBed.inject(ActiveBatchService).open(b24187);
    const crumb = await screen.findByTestId('batch-crumb');
    expect(crumb.textContent.replace(/\s+/g, ' ').trim()).toBe(
      'BATCH B-24187 · ORDER SO-9931 · MACHINE JET-04',
    );
  });

  it('should sign out from the operator chip and return to sign-in', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const user = userEvent.setup();
    const chip = screen.getByRole('button', { name: /Operator R. Karim/ });
    expect(chip.getAttribute('aria-expanded')).toBe('false');

    await user.click(chip);
    expect(chip.getAttribute('aria-expanded')).toBe('true');
    await user.click(screen.getByRole('button', { name: 'Sign out' }));

    httpMock()
      .expectOne((r) => r.method === 'DELETE' && r.url === '/api/session')
      .flush(null, { status: 204, statusText: 'No Content' });
    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/sign-in']);
    });
    expect(TestBed.inject(SessionService).signedIn()).toBe(false);
    expect(await screen.findByText('Not signed in')).toBeTruthy();
  });

  it('should close the operator menu with Escape', async () => {
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Operator R. Karim/ }));
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('button', { name: 'Sign out' })).toBeNull();
  });
});
