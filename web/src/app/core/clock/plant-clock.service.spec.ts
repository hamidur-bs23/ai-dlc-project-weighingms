import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { station } from '../../../testing/fixtures';
import { httpMock, provideTestHttp, settle } from '../../../testing/test-providers';
import { PlantClockService } from './plant-clock.service';

describe('PlantClockService', () => {
  let clock: PlantClockService;

  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'Date'] });
    // The terminal clock is 2 minutes behind the Api, and the browser is in UTC.
    vi.setSystemTime(new Date('2026-11-05T01:45:03Z'));
    TestBed.configureTestingModule({ providers: [provideTestHttp(), provideRouter([])] });
    clock = TestBed.inject(PlantClockService);
  });

  afterEach(() => {
    clock.stop();
    vi.useRealTimers();
  });

  const start = async () => {
    const started = clock.start();
    httpMock().expectOne('/api/station').flush(station);
    await started;
  };

  it('should show the Api time in the plant time zone, not the terminal time', async () => {
    await start();
    expect(clock.time()).toBe('07:47:03');
    expect(clock.stationId()).toBe('STATION 01');
  });

  it('should tick every second', async () => {
    await start();
    vi.advanceTimersByTime(2000);
    expect(clock.time()).toBe('07:47:05');
  });

  it('should format any instant as plant time', async () => {
    await start();
    expect(clock.format('2026-11-05T01:12:30Z')).toBe('07:12');
    expect(clock.format('2026-11-05T01:12:30Z', true)).toBe('07:12:30');
  });

  it('should show no clock rather than a wrong one when the station cannot be read', async () => {
    const started = clock.start();
    httpMock().expectOne('/api/station').flush({}, { status: 503, statusText: 'x' });
    await started;
    await settle();
    expect(clock.time()).toBeNull();
  });
});
