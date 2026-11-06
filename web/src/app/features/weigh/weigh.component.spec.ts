import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Subject } from 'rxjs';
import type { WeighingReading, WeighingSession } from '@core/api/api.types';
import { FunctionKeysService } from '@core/keyboard/function-keys.service';
import { ScannerService } from '@core/scanner/scanner.service';
import {
  b24187,
  recipe24187,
  scales,
  weighingSession,
  withAttempt,
  yellowReading,
} from '../../../testing/fixtures';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
} from '../../../testing/test-providers';
import { WeighComponent } from './weigh.component';

const SESSION = '/api/batches/B-24187/session';
/** A long poll interval so each test controls exactly one reading. */
const slowScales = { ...scales, pollIntervalMs: 600_000 };

describe('WeighComponent (screen 2 · Weigh)', () => {
  let scans: Subject<string>;
  let navigate: ReturnType<typeof vi.fn>;

  const press = (key: string) =>
    TestBed.inject(FunctionKeysService).handle(
      new KeyboardEvent('keydown', { key, cancelable: true }),
    );
  const lastRequest = (url: string) => vi.waitFor(() => httpMock().expectOne(url));

  /** Renders the screen, opens B-24187 with `session` and answers the first reading poll with `reading`. */
  async function open(session: WeighingSession, reading: WeighingReading | null) {
    scans = new Subject<string>();
    await render(WeighComponent, {
      providers: [
        provideTestHttp(),
        provideRouter([]),
        provideTestTransloco(),
        { provide: ScannerService, useValue: { scans: () => scans.asObservable() } },
      ],
      inputs: { batchId: 'B-24187' },
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true) as never;
    (await lastRequest('/api/batches/B-24187')).flush(b24187);
    (await lastRequest('/api/scales')).flush(slowScales);
    httpMock().expectOne('/api/batches/B-24187/recipe').flush(recipe24187);
    httpMock()
      .expectOne((r) => r.method === 'POST' && r.url === SESSION)
      .flush(session);
    if (reading) {
      (await lastRequest(`${SESSION}/reading`)).flush(reading);
    }
    await screen.findByRole('heading', {
      name: session.components.find((c) => c.sequence === session.activeSequence)?.name ?? '',
    });
  }

  const ready = () =>
    withAttempt(weighingSession(3), { lot: '2026-0387', container: 'C-12', tare: 214 });
  const confirmButton = () =>
    screen.getByRole<HTMLButtonElement>('button', { name: /OK · Confirm/ });
  const status = () => screen.getByTestId('status-line').textContent.replace(/\s+/g, ' ').trim();

  afterEach(() => {
    httpMock().verify();
  });

  // ── STEP 22: the boundary readings, as the server classifies them ──────────────
  it.each([
    [93.4, 'underweight', false, 'DISPENSE — 0.6 g REMAINING'],
    [93.5, 'withinRange', true, 'WITHIN TOLERANCE — PRESS OK TO CONFIRM'],
    [94.0, 'targetAchieved', true, 'TARGET ACHIEVED — PRESS OK TO CONFIRM'],
    [94.5, 'withinRange', true, 'WITHIN TOLERANCE — PRESS OK TO CONFIRM'],
    [94.6, 'overweight', false, 'OVER TOLERANCE — SURPLUS +0.6 g (+0.6%)'],
  ] as const)('STEP 22: %s g → %s, Confirm enabled = %s', async (net, cls, enabled, text) => {
    await open(
      ready(),
      yellowReading(net, cls, { surplus: cls === 'overweight' ? 0.6 : undefined }),
    );

    expect(screen.getByTestId('weighed-now').textContent.trim()).toBe(`${net.toFixed(1)} g`);
    expect(status()).toContain(text);
    expect(confirmButton().disabled).toBe(!enabled);
  });

  it('should show the header, the automatic scale badge and the attempt', async () => {
    await open(ready(), yellowReading(55.3, 'underweight'));

    expect(
      screen.getByText(/DYE-4471 · LOT 2026-0387 · CONTAINER C-12 · TARE 214.0 g/),
    ).toBeTruthy();
    const badge = screen.getByTestId('scale-badge');
    expect(badge.textContent).toContain('3 kg precision');
    expect(badge.textContent).toContain('automatic');
    expect(screen.getByTestId('still-to-weigh').textContent.trim()).toBe('38.7 g');
    expect(screen.getByText('TOLERANCE ±0.5 g (93.5 – 94.5)')).toBeTruthy();
  });

  it('STEP 21: Sodium Sulphate is weighed on the 32 kg floor scale', async () => {
    await open(weighingSession(4), null);

    expect(screen.getByTestId('scale-badge').textContent).toContain('32 kg floor');
    (await lastRequest(`${SESSION}/reading`)).flush({
      ...yellowReading(0, 'underweight'),
      sequence: 4,
    });
  });

  it('should show the amber near-target hint at ≥ 90 % of the target', async () => {
    await open(ready(), yellowReading(86.0, 'underweight'));

    expect(status()).toBe('DISPENSE — 8.0 g REMAINING · NEAR TARGET');
    expect(screen.getByTestId('status-line').getAttribute('data-zone')).toBe('near');
  });

  it('should label an unstable reading and say why Confirm is disabled', async () => {
    await open(ready(), yellowReading(94.0, 'targetAchieved', { stable: false }));

    expect(screen.getByText('UNSTABLE')).toBeTruthy();
    expect(screen.getByText('Wait for a stable reading.')).toBeTruthy();
    expect(confirmButton().disabled).toBe(true);
  });

  it('should list what is missing before Confirm is possible', async () => {
    await open(
      weighingSession(3),
      yellowReading(94.2, 'withinRange', { missing: ['lot', 'container', 'tare'] }),
    );

    expect(screen.getByText('Enter or scan the lot.')).toBeTruthy();
    expect(screen.getByText('Enter the container.')).toBeTruthy();
    expect(screen.getByText('Tare the empty container (F2).')).toBeTruthy();
    expect(confirmButton().disabled).toBe(true);
  });

  it('should show the surplus and the recovery actions on overshoot, without recalculation', async () => {
    await open(ready(), yellowReading(96.4, 'overweight', { surplus: 2.4 }));

    expect(status()).toBe('OVER TOLERANCE — SURPLUS +2.4 g (+2.6%)');
    expect(screen.getByTestId('surplus').textContent).toContain('ask a supervisor to override');
    expect(screen.getByRole('button', { name: 'Supervisor override' })).toBeTruthy();
    expect(screen.queryByRole('table')).toBeNull(); // no recalculation table (PD-05)
  });

  it('should not offer override within tolerance', async () => {
    await open(ready(), yellowReading(94.2, 'withinRange'));

    expect(screen.queryByRole('button', { name: 'Supervisor override' })).toBeNull();
  });

  it('should pre-fill the suggested container and save lot and container on Enter', async () => {
    await open(weighingSession(3), yellowReading(0, 'underweight'));
    const user = userEvent.setup();

    expect(screen.getByLabelText<HTMLInputElement>('Container').value).toBe('C-12');
    await user.type(screen.getByLabelText('Lot (scan or type)'), '  2026-0387 {Enter}');

    const lot = await lastRequest(`${SESSION}/attempt/lot`);
    expect(lot.request.body).toEqual({ sequence: 3, lot: '2026-0387' });
    lot.flush(withAttempt(weighingSession(3), { lot: '2026-0387' }));

    await user.click(screen.getByLabelText('Container'));
    await user.keyboard('{Enter}');
    const container = await lastRequest(`${SESSION}/attempt/container`);
    expect(container.request.body).toEqual({ sequence: 3, container: 'C-12' });
    container.flush(withAttempt(weighingSession(3), { lot: '2026-0387', container: 'C-12' }));
    expect(await screen.findByText(/LOT 2026-0387 · CONTAINER C-12/)).toBeTruthy();
  });

  it('should fill the lot from a scan', async () => {
    await open(weighingSession(3), yellowReading(0, 'underweight'));

    scans.next('2026-0412');

    const lot = await lastRequest(`${SESSION}/attempt/lot`);
    expect(lot.request.body).toEqual({ sequence: 3, lot: '2026-0412' });
    lot.flush(withAttempt(weighingSession(3), { lot: '2026-0412' }));
    expect(await screen.findByText(/LOT 2026-0412/)).toBeTruthy();
  });

  it('should confirm with F1 only when the server allows it', async () => {
    await open(ready(), yellowReading(93.4, 'underweight'));
    press('F1');
    httpMock().expectNone(`${SESSION}/confirmation`);
  });

  it('should confirm with F1 and move to the next component', async () => {
    await open(ready(), yellowReading(94.2, 'withinRange'));

    press('F1');

    const req = await lastRequest(`${SESSION}/confirmation`);
    expect(req.request.body).toEqual({ sequence: 3 });
    req.flush({ record: {}, session: weighingSession(4, { progress: { done: 1, total: 5 } }) });
    expect(await screen.findByRole('heading', { name: 'Sodium Sulphate' })).toBeTruthy();
    (await lastRequest(`${SESSION}/reading`)).flush({
      ...yellowReading(0, 'underweight'),
      sequence: 4,
    });
  });

  it('should tare with F2 and label it Re-tare once tared', async () => {
    await open(weighingSession(3), yellowReading(0, 'underweight'));
    expect(screen.getByRole('button', { name: /^Tare/ })).toBeTruthy();

    press('F2');
    (await lastRequest(`${SESSION}/tare`)).flush(withAttempt(weighingSession(3), { tare: 214 }));

    expect(await screen.findByRole('button', { name: /^Re-tare/ })).toBeTruthy();
  });

  it('should ask before quitting an attempt with captured data, then quit with F4', async () => {
    await open(ready(), yellowReading(50, 'underweight'));

    press('F4');
    httpMock().expectNone(`${SESSION}/quit`);
    expect(await screen.findByRole('button', { name: /Discard attempt\?/ })).toBeTruthy();

    press('F4');
    (await lastRequest(`${SESSION}/quit`)).flush(weighingSession(null));
  });

  it('should quit straight away when nothing was captured', async () => {
    await open(weighingSession(3), yellowReading(0, 'underweight'));

    press('F4');

    (await lastRequest(`${SESSION}/quit`)).flush(weighingSession(null));
  });

  it('should keep weighing when the operator cancels the quit', async () => {
    await open(ready(), yellowReading(50, 'underweight'));
    press('F4');
    await userEvent.setup().click(await screen.findByRole('button', { name: 'Keep weighing' }));

    expect(screen.queryByRole('button', { name: /Discard attempt\?/ })).toBeNull();
    httpMock().expectNone(`${SESSION}/quit`);
  });

  it('should open the override dialog when offered', async () => {
    await open(ready(), yellowReading(96.4, 'overweight', { surplus: 2.4 }));

    await userEvent.setup().click(screen.getByRole('button', { name: 'Supervisor override' }));

    expect(await screen.findByRole('heading', { name: 'Supervisor override' })).toBeTruthy();
    expect(screen.getByLabelText('PIN').getAttribute('type')).toBe('password');
  });

  it('should open the summary when the last record completes the batch', async () => {
    await open(ready(), yellowReading(94.2, 'withinRange'));

    press('F1');
    (await lastRequest(`${SESSION}/confirmation`)).flush({
      record: {},
      session: weighingSession(null, {
        status: 'completed',
        readyToComplete: true,
        progress: { done: 5, total: 5 },
      }),
    });

    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/batches', 'B-24187', 'summary']);
    });
  });

  it('should leave the batch and return to the queue', async () => {
    await open(weighingSession(3), yellowReading(0, 'underweight'));

    await userEvent.setup().click(screen.getByRole('button', { name: 'Back to queue' }));
    (await lastRequest(`${SESSION}/leave`)).flush(weighingSession(null));

    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/batches']);
    });
  });

  it('should show a command error with its Ref', async () => {
    await open(weighingSession(3), yellowReading(0, 'underweight'));

    press('F2');
    (await lastRequest(`${SESSION}/tare`)).flush(problem('Scale.Unavailable'), {
      status: 503,
      statusText: 'x',
    });

    expect((await screen.findByRole('alert')).textContent).toContain(
      'The scale is not responding.',
    );
  });

  it('should say when no tolerance is defined', async () => {
    const blocked = weighingSession(3);
    blocked.components[2] = {
      ...blocked.components[2],
      tolerance: { status: 'blocked', source: null, plusMinus: null, lower: null, upper: null },
    };
    await open(blocked, {
      ...yellowReading(50, null, { missing: ['tolerance'] }),
      band: null,
      toleranceStatus: 'blocked',
    });

    expect(status()).toBe('NO TOLERANCE DEFINED — ASK A SUPERVISOR');
  });
});
