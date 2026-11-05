import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { operatorSession } from '../../../testing/fixtures';
import { httpMock, provideTestHttp } from '../../../testing/test-providers';
import { signedInGuard, signedOutGuard } from './auth.guards';
import { SessionService } from './session.service';

const run = (guard: typeof signedInGuard) =>
  TestBed.runInInjectionContext(() =>
    guard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  );

describe('station guards', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideTestHttp(), provideRouter([])] });
  });

  it('should send a station without a session to sign-in', () => {
    const result = run(signedInGuard) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/sign-in');
  });

  it('should let a signed-in operator in, and skip sign-in for them', async () => {
    const session = TestBed.inject(SessionService);
    const restore = session.restore();
    httpMock().expectOne('/api/session').flush(operatorSession);
    await restore;

    expect(run(signedInGuard)).toBe(true);
    expect(TestBed.inject(Router).serializeUrl(run(signedOutGuard) as UrlTree)).toBe('/batches');
  });
});
