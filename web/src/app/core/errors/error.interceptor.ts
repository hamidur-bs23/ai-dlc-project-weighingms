import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { SessionService } from '@core/auth/session.service';
import { AppError } from './app-error';

interface ProblemBody {
  errorCode?: unknown;
  traceId?: unknown;
}

/** Turns an HTTP failure into an {@link AppError} carrying the ProblemDetails code and trace ID. */
export function toAppErrorFromHttp(error: HttpErrorResponse): AppError {
  if (error.status === 0) {
    return new AppError(AppError.networkCode, 0, null);
  }

  const body = (
    typeof error.error === 'object' && error.error !== null ? error.error : {}
  ) as ProblemBody;
  const code = typeof body.errorCode === 'string' ? body.errorCode : AppError.unknownCode;
  const traceId = typeof body.traceId === 'string' ? body.traceId : null;
  return new AppError(code, error.status, traceId);
}

/**
 * Maps every Api error to an AppError. A 401 on any call other than the session itself means the
 * session has ended (expired, or replaced at the station): the UI drops it and returns to sign-in.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const session = inject(SessionService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      if (error.status === 401 && !req.url.startsWith('/api/session')) {
        session.clear();
        void router.navigate(['/sign-in']);
      }

      return throwError(() => toAppErrorFromHttp(error));
    }),
  );
};
