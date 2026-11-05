import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { health, scales } from '../../../testing/fixtures';
import { httpMock, provideTestHttp, provideTestTimings } from '../../../testing/test-providers';
import { DeviceHealthService } from './device-health.service';

describe('DeviceHealthService', () => {
  let service: DeviceHealthService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [provideTestHttp(), provideRouter([]), provideTestTimings(5000, 500)],
    });
    service = TestBed.inject(DeviceHealthService);
  });

  afterEach(() => {
    service.stop();
    httpMock().verify();
    vi.useRealTimers();
  });

  const start = () => {
    service.start();
    httpMock().expectOne('/api/scales').flush(scales);
    vi.advanceTimersByTime(0);
  };

  it('should list every device with its scale name and state', () => {
    start();
    httpMock().expectOne('/api/devices/health').flush(health());

    expect(service.devices().map((d) => [d.device, d.name, d.state])).toEqual([
      ['scale-3kg', '3 kg precision', 'ready'],
      ['scale-32kg', '32 kg floor', 'ready'],
      ['scanner', null, 'ready'],
      ['printer', null, 'ready'],
      ['orgatex', null, 'ready'],
    ]);
  });

  it('should show a faulted device as offline on the next poll', () => {
    start();
    httpMock().expectOne('/api/devices/health').flush(health());

    vi.advanceTimersByTime(5000);
    httpMock()
      .expectOne('/api/devices/health')
      .flush(health({ 'scale-32kg': 'offline', orgatex: 'offline' }));

    const states = Object.fromEntries(service.devices().map((d) => [d.device, d.state]));
    expect(states).toMatchObject({
      'scale-32kg': 'offline',
      orgatex: 'offline',
      'scale-3kg': 'ready',
    });
  });

  it('should never claim ready when the Api cannot be reached', () => {
    start();
    httpMock().expectOne('/api/devices/health').flush(health());
    vi.advanceTimersByTime(5000);
    httpMock().expectOne('/api/devices/health').error(new ProgressEvent('error'), { status: 0 });

    expect(service.devices().every((d) => d.state === 'unknown')).toBe(true);
  });

  it('should stop polling and clear the strip on stop', () => {
    start();
    httpMock().expectOne('/api/devices/health').flush(health());
    service.stop();
    vi.advanceTimersByTime(20000);
    httpMock().expectNone('/api/devices/health');
    expect(service.devices()).toEqual([]);
  });
});
