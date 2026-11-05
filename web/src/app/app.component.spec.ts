import { DestroyRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { render, screen } from '@testing-library/angular';
import { SessionService } from '@core/auth/session.service';
import { FunctionKeysService } from '@core/keyboard/function-keys.service';
import { health, operatorSession, scales, station } from '../testing/fixtures';
import {
  httpMock,
  provideTestHttp,
  provideTestTimings,
  provideTestTransloco,
} from '../testing/test-providers';
import { App } from './app.component';

describe('App (station shell)', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideTestHttp(),
        provideRouter([]),
        provideTestTransloco(),
        provideTestTimings(60_000, 60_000),
      ],
    });
  });

  afterEach(() => {
    httpMock().verify();
  });

  it('should show the top bar, the screen and the status strip', async () => {
    await render(App);
    expect(screen.getByRole('banner')).toBeTruthy();
    expect(screen.getByRole('main')).toBeTruthy();
    expect(screen.getByRole('contentinfo')).toBeTruthy();
  });

  it('should poll device health and read the plant clock only while signed in', async () => {
    await render(App);
    httpMock().expectNone('/api/devices/health');

    const restore = TestBed.inject(SessionService).restore();
    httpMock().expectOne('/api/session').flush(operatorSession);
    await restore;

    await vi.waitFor(() => {
      httpMock().expectOne('/api/scales').flush(scales);
    });
    await vi.waitFor(() => {
      httpMock().expectOne('/api/devices/health').flush(health());
    });
    httpMock().expectOne('/api/station').flush(station);
    expect(await screen.findByText(/SCALE 3 kg precision · READY/)).toBeTruthy();

    TestBed.inject(SessionService).clear();
    await vi.waitFor(() => {
      expect(screen.queryByText(/SCALE 3 kg precision/)).toBeNull();
    });
  });

  it('should forward function keys to the current screen', async () => {
    const run = vi.fn();
    const { fixture } = await render(App);
    TestBed.inject(FunctionKeysService).register(
      'F2',
      { run },
      fixture.componentRef.injector.get(DestroyRef),
    );
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'F2', cancelable: true }));
    expect(run).toHaveBeenCalledOnce();
  });
});
