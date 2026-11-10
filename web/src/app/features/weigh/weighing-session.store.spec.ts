import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  recipe24187,
  scales,
  weighingSession,
  withAttempt,
  yellowReading,
} from '../../../testing/fixtures';
import { httpMock, problem, provideTestHttp, settle } from '../../../testing/test-providers';
import { WeighingSessionStore } from './weighing-session.store';

const SESSION = '/api/batches/B-24187/session';

describe('WeighingSessionStore', () => {
  let store: InstanceType<typeof WeighingSessionStore>;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [provideTestHttp(), provideRouter([]), WeighingSessionStore],
    });
    store = TestBed.inject(WeighingSessionStore);
  });

  afterEach(() => {
    store.stop();
    httpMock().verify();
    vi.useRealTimers();
  });

  /** Opens B-24187 with Yellow RGB active; the first reading poll is left pending. */
  const open = async (session = weighingSession(3)) => {
    const opening = store.open('B-24187');
    httpMock().expectOne('/api/scales').flush(scales);
    httpMock().expectOne('/api/batches/B-24187/recipe').flush(recipe24187);
    httpMock()
      .expectOne((r) => r.method === 'POST' && r.url === SESSION)
      .flush(session);
    await opening;
    vi.advanceTimersByTime(0);
  };

  it('should load the scales, the recipe (STEP 19 flow) and start or resume the session', async () => {
    await open();

    expect(store.session()?.batchId).toBe('B-24187');
    expect(store.active()?.name).toBe('Remazol Yellow RGB');
    expect(store.scaleNames()).toEqual({
      'scale-3kg': '3 kg precision',
      'scale-32kg': '32 kg floor',
    });
    expect(store.pollMs()).toBe(250);
    expect(store.colourOf()(3)).toBe('#e3b51d');
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(55.3, 'underweight'));
  });

  it('should select the suggested component when nothing is active (PD-07)', async () => {
    const opening = store.open('B-24187');
    httpMock().expectOne('/api/scales').flush(scales);
    httpMock().expectOne('/api/batches/B-24187/recipe').flush(recipe24187);
    httpMock()
      .expectOne((r) => r.method === 'POST' && r.url === SESSION)
      .flush(weighingSession(null));
    await settle();
    const select = httpMock().expectOne(`${SESSION}/selection`);
    expect(select.request.body).toEqual({ sequence: 1 });
    select.flush(weighingSession(1));
    await opening;
    vi.advanceTimersByTime(0);

    expect(store.active()?.sequence).toBe(1);
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(0, 'underweight'));
  });

  it('should poll the reading at the server interval, one request at a time', async () => {
    await open();
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush(yellowReading(55.3, 'underweight', { stable: false }));
    expect(store.activeReading()?.net?.value).toBe(55.3);

    vi.advanceTimersByTime(250);
    const slow = httpMock().expectOne(`${SESSION}/reading`);
    vi.advanceTimersByTime(250); // still in flight: no second request
    httpMock().expectNone(`${SESSION}/reading`);
    slow.flush(yellowReading(94.2, 'withinRange', { missing: ['lot', 'container', 'tare'] }));

    expect(store.activeReading()?.class).toBe('withinRange');
    expect(store.canConfirm()).toBe(false); // the server says what is missing
  });

  it('should keep polling after a failed reading and show the device error', async () => {
    await open();
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush(problem('Scale.Unavailable'), { status: 503, statusText: 'x' });
    expect(store.readingError()?.code).toBe('Scale.Unavailable');

    vi.advanceTimersByTime(250);
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(94.0, 'targetAchieved'));
    expect(store.readingError()).toBeNull();
    expect(store.canConfirm()).toBe(true);
  });

  it('should take can-confirm and the override offer only from the server', async () => {
    await open();
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush(yellowReading(96.4, 'overweight', { surplus: 2.4 }));

    expect(store.canConfirm()).toBe(false);
    expect(store.overrideOffered()).toBe(true);
  });

  it('should ignore a reading of another component', async () => {
    await open();
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush({ ...yellowReading(94.0, 'targetAchieved'), sequence: 4 });

    expect(store.activeReading()).toBeNull();
    expect(store.canConfirm()).toBe(false);
  });

  it('should queue commands in order and never drop one (container blur + lot Enter)', async () => {
    await open();
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(0, 'underweight'));

    const container = store.captureContainer('C-12');
    const lot = store.captureLot('2026-0387');
    expect(store.isBusy()).toBe(true);
    await settle();

    const first = httpMock().expectOne(`${SESSION}/attempt/container`);
    httpMock().expectNone(`${SESSION}/attempt/lot`); // waits for the first
    expect(first.request.body).toEqual({ sequence: 3, container: 'C-12' });
    first.flush(withAttempt(weighingSession(3), { container: 'C-12' }));
    await settle();
    const second = httpMock().expectOne(`${SESSION}/attempt/lot`);
    expect(second.request.body).toEqual({ sequence: 3, lot: '2026-0387' });
    second.flush(withAttempt(weighingSession(3), { container: 'C-12', lot: '2026-0387' }));

    expect(await container).toBe(true);
    expect(await lot).toBe(true);
    expect(store.active()?.attempt?.lot).toBe('2026-0387');
    expect(store.isBusy()).toBe(false);
  });

  it('should show a command error in the banner and carry on with the next command', async () => {
    await open();
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(0, 'underweight'));

    const tare = store.tare();
    await settle();
    httpMock()
      .expectOne(`${SESSION}/tare`)
      .flush(problem('Scale.Unavailable'), { status: 503, statusText: 'x' });

    expect(await tare).toBe(false);
    expect(store.commandError()?.code).toBe('Scale.Unavailable');
    expect(store.isBusy()).toBe(false);
  });

  it('should confirm the active component by name and follow the next active one', async () => {
    await open();
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(94.2, 'withinRange'));

    const confirming = store.confirm();
    await settle();
    const req = httpMock().expectOne(`${SESSION}/confirmation`);
    expect(req.request.body).toEqual({ sequence: 3 });
    req.flush({ record: {}, session: weighingSession(4, { progress: { done: 1, total: 5 } }) });
    await confirming;
    vi.advanceTimersByTime(0);

    expect(store.active()?.code).toBe('AUX-0101');
    expect(store.activeReading()).toBeNull(); // the old reading belongs to component 3
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush({ ...yellowReading(0, 'underweight'), sequence: 4 });
  });

  it('should stop polling once the batch is completed', async () => {
    await open();
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(94.2, 'withinRange'));

    const confirming = store.confirm();
    await settle();
    httpMock()
      .expectOne(`${SESSION}/confirmation`)
      .flush({
        record: {},
        session: weighingSession(null, { status: 'completed', readyToComplete: true }),
      });
    await confirming;
    vi.advanceTimersByTime(5000);

    expect(store.completed()).toBe(true);
    httpMock().expectNone(`${SESSION}/reading`);
  });

  it('should return an override error to the dialog instead of the banner', async () => {
    await open();
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush(yellowReading(96.4, 'overweight', { surplus: 2.4 }));

    const overriding = store.override('grant-1', 'Accepted');
    const req = httpMock().expectOne(`${SESSION}/override`);
    expect(req.request.body).toEqual({ sequence: 3, grantId: 'grant-1', reason: 'Accepted' });
    req.flush(problem('Auth.FourEyesRequired'), { status: 403, statusText: 'x' });

    expect((await overriding)?.code).toBe('Auth.FourEyesRequired');
    expect(store.commandError()).toBeNull();
  });

  it('should report a session that cannot be opened', async () => {
    const opening = store.open('B-24187');
    httpMock().expectOne('/api/scales').flush(scales);
    httpMock()
      .expectOne('/api/batches/B-24187/recipe')
      .flush(problem('OrgaTex.Unavailable'), { status: 503, statusText: 'x' });
    httpMock()
      .expectOne((r) => r.method === 'POST' && r.url === SESSION)
      .flush(weighingSession(3));
    await opening;

    expect(store.loadError()?.code).toBe('OrgaTex.Unavailable');
    httpMock().expectNone(`${SESSION}/reading`);
  });

  it('should send quit and leave for the batch', async () => {
    await open(withAttempt(weighingSession(3), { lot: 'L', tare: 214 }));
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(0, 'underweight'));

    const quit = store.quit();
    await settle();
    httpMock().expectOne(`${SESSION}/quit`).flush(weighingSession(null));
    await quit;
    const leave = store.leave();
    await settle();
    httpMock().expectOne(`${SESSION}/leave`).flush(weighingSession(null));
    await leave;

    expect(store.active()).toBeNull();
  });

  // ── Story 011: a failed read leaves no known weight ─────────────────────────────
  it('should clear the shown weight when a reading fails, so an old one never looks live', async () => {
    await open();
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(94.0, 'targetAchieved'));
    expect(store.canConfirm()).toBe(true);

    vi.advanceTimersByTime(250);
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush(problem('Scale.Unavailable'), { status: 503, statusText: 'x' });

    expect(store.activeReading()).toBeNull();
    expect(store.canConfirm()).toBe(false);
    expect(store.scaleOffline()).toBe(true);
    expect(store.canTare()).toBe(false);
  });

  it('should stop tare on any failed read, but call only Scale.Unavailable offline', async () => {
    await open();
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush(problem('Unknown'), { status: 500, statusText: 'x' });

    expect(store.scaleOffline()).toBe(false);
    expect(store.canTare()).toBe(false);
  });

  it('should let tare again once readings return', async () => {
    await open();
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush(problem('Scale.Unavailable'), { status: 503, statusText: 'x' });
    expect(store.canTare()).toBe(false);

    vi.advanceTimersByTime(250);
    httpMock().expectOne(`${SESSION}/reading`).flush(yellowReading(0, 'underweight'));

    expect(store.scaleOffline()).toBe(false);
    expect(store.canTare()).toBe(true);
  });

  it('should forget a failed read when another component, on another scale, becomes active', async () => {
    await open();
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush(problem('Scale.Unavailable'), { status: 503, statusText: 'x' });
    expect(store.scaleOffline()).toBe(true);

    const selecting = store.select(4);
    await settle();
    httpMock().expectOne(`${SESSION}/selection`).flush(weighingSession(4));
    await selecting;

    expect(store.scaleOffline()).toBe(false);
    expect(store.canTare()).toBe(true);
    vi.advanceTimersByTime(0);
    httpMock()
      .expectOne(`${SESSION}/reading`)
      .flush({ ...yellowReading(0, 'underweight'), sequence: 4 });
  });
});
