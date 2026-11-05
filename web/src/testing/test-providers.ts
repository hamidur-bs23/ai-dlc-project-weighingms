import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { EnvironmentProviders, Provider, importProvidersFrom } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { errorInterceptor } from '@core/errors/error.interceptor';
import { STATION_UI_CONFIG } from '@core/station-config';
import en from '../../public/i18n/en.json';

/** The real English translations, loaded synchronously, so specs read what operators read. */
export function provideTestTransloco(): EnvironmentProviders {
  return importProvidersFrom(
    TranslocoTestingModule.forRoot({
      langs: { en },
      translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
      preloadLangs: true,
    }),
  );
}

/** HttpClient with the app's error interceptor, against HttpTestingController. */
export function provideTestHttp(): (Provider | EnvironmentProviders)[] {
  return [provideHttpClient(withInterceptors([errorInterceptor])), provideHttpClientTesting()];
}

/** Short, fixed poll intervals for fake-timer specs. */
export function provideTestTimings(healthPollMs = 5000, scannerPollMs = 500): Provider {
  return { provide: STATION_UI_CONFIG, useValue: { healthPollMs, scannerPollMs } };
}

export function httpMock(): HttpTestingController {
  return TestBed.inject(HttpTestingController);
}

/** A ProblemDetails body as WeighingMS.Api sends it. */
export function problem(
  errorCode: string,
  traceId = '00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01',
) {
  return { type: `https://weighingms/errors/${errorCode}`, title: errorCode, errorCode, traceId };
}

/** Lets pending promise continuations (async component methods) run. */
export async function settle(): Promise<void> {
  for (let i = 0; i < 5; i++) {
    await Promise.resolve();
  }
}
