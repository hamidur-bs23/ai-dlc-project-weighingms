import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { operatorSession } from '../../../testing/fixtures';
import { httpMock, provideTestHttp, settle } from '../../../testing/test-providers';
import { SessionService } from './session.service';
import { stationStartup } from './station-startup';

describe('stationStartup', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideTestHttp(), provideRouter([])] });
  });

  afterEach(() => {
    httpMock().verify();
  });

  it('should fetch the XSRF cookie, then restore the session', async () => {
    const started = TestBed.runInInjectionContext(() => stationStartup());
    httpMock().expectOne('/api/antiforgery').flush(null, { status: 204, statusText: 'No Content' });
    await settle();
    httpMock().expectOne('/api/session').flush(operatorSession);
    await started;
    expect(TestBed.inject(SessionService).session()?.badgeId).toBe('OP-1001');
  });

  it('should still open the station (on sign-in) when the Api is unreachable', async () => {
    const started = TestBed.runInInjectionContext(() => stationStartup());
    httpMock().expectOne('/api/antiforgery').error(new ProgressEvent('error'), { status: 0 });
    await expect(started).resolves.toBeUndefined();
    expect(TestBed.inject(SessionService).signedIn()).toBe(false);
  });
});
