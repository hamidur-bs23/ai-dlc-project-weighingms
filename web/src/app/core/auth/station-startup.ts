import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SessionService } from './session.service';

/**
 * App start-up: fetch the XSRF-TOKEN cookie (antiforgery, echoed as X-XSRF-TOKEN by HttpClient),
 * then restore any session. If the Api is unreachable the station still opens on sign-in, which
 * reports the outage when the operator tries to sign in.
 */
export async function stationStartup(): Promise<void> {
  const http = inject(HttpClient);
  const session = inject(SessionService);
  try {
    await firstValueFrom(http.get('/api/antiforgery'));
    await session.restore();
  } catch {
    session.clear();
  }
}
