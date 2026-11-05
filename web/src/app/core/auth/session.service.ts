import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Session, SignInRequest } from '@core/api/api.types';
import { AppError } from '@core/errors/app-error';

/** The station session (ADR-009): who is signed in, restored from the Api cookie on start. */
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly current = signal<Session | null>(null);

  readonly session = this.current.asReadonly();
  readonly signedIn = computed(() => this.current() !== null);

  /** Reads the session from the cookie; no session (401) is not an error. */
  async restore(): Promise<void> {
    try {
      this.current.set(await firstValueFrom(this.http.get<Session>('/api/session')));
    } catch (error) {
      if (error instanceof AppError && error.status === 401) {
        this.current.set(null);
        return;
      }
      throw error;
    }
  }

  async signIn(request: SignInRequest): Promise<Session> {
    // The XSRF cookie comes from start-up; fetch it again if the Api was down then.
    if (!document.cookie.split('; ').some((c) => c.startsWith('XSRF-TOKEN='))) {
      await firstValueFrom(this.http.get('/api/antiforgery'));
    }
    const session = await firstValueFrom(this.http.post<Session>('/api/session', request));
    this.current.set(session);
    return session;
  }

  async signOut(): Promise<void> {
    try {
      await firstValueFrom(this.http.delete<unknown>('/api/session'));
    } finally {
      this.current.set(null);
    }
  }

  /** Forgets the session locally (the Api already ended it). */
  clear(): void {
    this.current.set(null);
  }
}
