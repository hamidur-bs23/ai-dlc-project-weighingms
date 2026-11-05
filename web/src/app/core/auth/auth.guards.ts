import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from './session.service';

/** Every station screen needs a signed-in operator; without one the station shows sign-in first. */
export const signedInGuard: CanActivateFn = () =>
  inject(SessionService).signedIn() || inject(Router).createUrlTree(['/sign-in']);

/** Sign-in is skipped when a session already exists. */
export const signedOutGuard: CanActivateFn = () =>
  !inject(SessionService).signedIn() || inject(Router).createUrlTree(['/batches']);
