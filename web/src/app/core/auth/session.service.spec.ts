import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { operatorSession } from '../../../testing/fixtures';
import { httpMock, problem, provideTestHttp, settle } from '../../../testing/test-providers';
import { SessionService } from './session.service';

describe('SessionService', () => {
  let session: SessionService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideTestHttp(), provideRouter([])] });
    session = TestBed.inject(SessionService);
    document.cookie = 'XSRF-TOKEN=test-token';
  });

  afterEach(() => {
    httpMock().verify();
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT';
  });

  it('should restore the session from the cookie', async () => {
    const restore = session.restore();
    httpMock().expectOne('/api/session').flush(operatorSession);
    await restore;
    expect(session.session()?.displayName).toBe('R. Karim');
    expect(session.signedIn()).toBe(true);
  });

  it('should treat 401 on restore as no session, not an error', async () => {
    const restore = session.restore();
    httpMock()
      .expectOne('/api/session')
      .flush(problem('Auth.SignInRequired'), { status: 401, statusText: 'x' });
    await restore;
    expect(session.signedIn()).toBe(false);
  });

  it('should sign in with the badge and shift', async () => {
    const signIn = session.signIn({ badgeId: 'OP-1001', shift: 'A' });
    const req = httpMock().expectOne('/api/session');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ badgeId: 'OP-1001', shift: 'A' });
    req.flush(operatorSession);
    await signIn;
    expect(session.session()?.badgeId).toBe('OP-1001');
  });

  it('should fetch the XSRF cookie first if start-up could not', async () => {
    document.cookie = 'XSRF-TOKEN=; expires=Thu, 01 Jan 1970 00:00:00 GMT';
    const signIn = session.signIn({ badgeId: 'OP-1001', shift: null });
    httpMock().expectOne('/api/antiforgery').flush(null, { status: 204, statusText: 'No Content' });
    await settle();
    httpMock().expectOne('/api/session').flush(operatorSession);
    await signIn;
    expect(session.signedIn()).toBe(true);
  });

  it('should forget the session on sign-out even if the Api fails', async () => {
    const restore = session.restore();
    httpMock().expectOne('/api/session').flush(operatorSession);
    await restore;

    const signOut = session.signOut();
    httpMock()
      .expectOne((r) => r.method === 'DELETE')
      .flush(problem('Unknown'), { status: 500, statusText: 'x' });
    await expect(signOut).rejects.toBeTruthy();
    expect(session.signedIn()).toBe(false);
  });
});
