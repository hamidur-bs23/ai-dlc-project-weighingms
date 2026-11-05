import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { SessionService } from '@core/auth/session.service';
import { httpMock, problem, provideTestHttp } from '../../../testing/test-providers';
import { AppError } from './app-error';

describe('errorInterceptor', () => {
  let http: HttpClient;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideTestHttp(), provideRouter([])] });
    http = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    httpMock().verify();
  });

  const failWith = async (
    url: string,
    status: number,
    body: object | string,
  ): Promise<AppError> => {
    const call = firstValueFrom(http.get(url));
    httpMock().expectOne(url).flush(body, { status, statusText: 'x' });
    try {
      await call;
    } catch (e) {
      return e as AppError;
    }
    throw new Error('expected the call to fail');
  };

  it('should map ProblemDetails to the error code, status and trace id', async () => {
    const error = await failWith('/api/batches', 503, problem('OrgaTex.Unavailable'));
    expect(error).toBeInstanceOf(AppError);
    expect(error.code).toBe('OrgaTex.Unavailable');
    expect(error.status).toBe(503);
    expect(error.ref).toBe('4bf92f35');
  });

  it('should map a body without an error code to Unknown', async () => {
    const error = await failWith('/api/batches', 500, 'Internal error');
    expect(error.code).toBe(AppError.unknownCode);
    expect(error.traceId).toBeNull();
  });

  it('should map an unreachable Api to Network.Unavailable', async () => {
    const call = firstValueFrom(http.get('/api/batches'));
    httpMock().expectOne('/api/batches').error(new ProgressEvent('error'), { status: 0 });
    await expect(call).rejects.toMatchObject({ code: AppError.networkCode, status: 0 });
  });

  it('should end the session and go to sign-in on a 401 from another endpoint', async () => {
    const session = TestBed.inject(SessionService);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const clear = vi.spyOn(session, 'clear');

    await failWith('/api/batches', 401, problem('Auth.SignInRequired'));

    expect(clear).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/sign-in']);
  });

  it('should leave a 401 from the session endpoint to the caller', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const error = await failWith('/api/session', 401, problem('Auth.BadgeNotRecognised'));
    expect(error.code).toBe('Auth.BadgeNotRecognised');
    expect(navigate).not.toHaveBeenCalled();
  });
});
