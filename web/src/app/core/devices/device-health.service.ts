import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { Subscription, catchError, of, switchMap, timer } from 'rxjs';
import type { DeviceHealthList, ScaleList } from '@core/api/api.types';
import { STATION_UI_CONFIG } from '@core/station-config';

export type DeviceKind = 'scale' | 'scanner' | 'printer' | 'orgatex';
export type DeviceState = 'ready' | 'offline' | 'unknown';

/** One status-strip entry. `name` is the scale's name from the Api; other devices use their kind. */
export interface DeviceStat {
  device: string;
  kind: DeviceKind;
  name: string | null;
  state: DeviceState;
}

/**
 * Device health for the status strip (story 001): polls `GET /api/devices/health` while a session
 * exists. If the Api itself cannot be reached, every device shows as unknown rather than ready.
 */
@Injectable({ providedIn: 'root' })
export class DeviceHealthService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(STATION_UI_CONFIG);
  private readonly stats = signal<readonly DeviceStat[]>([]);
  private readonly unsyncedCount = signal(0);
  private scaleNames = new Map<string, string>();
  private polling: Subscription | null = null;

  readonly devices = this.stats.asReadonly();
  /** OrgaTEX write-backs not yet sent (pending or sync failed), for the strip (bolt 011). */
  readonly unsynced = this.unsyncedCount.asReadonly();

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.stop();
    });
  }

  start(): void {
    if (this.polling) {
      return;
    }

    this.http.get<ScaleList>('/api/scales').subscribe({
      next: (list) => {
        this.scaleNames = new Map(list.items.map((s) => [s.scaleId, s.name]));
        this.stats.update((current) => current.map((d) => this.withName(d)));
      },
      error: () => undefined, // names are cosmetic; the strip falls back to the device id
    });

    this.polling = timer(0, this.config.healthPollMs)
      .pipe(
        switchMap(() =>
          this.http.get<DeviceHealthList>('/api/devices/health').pipe(catchError(() => of(null))),
        ),
      )
      .subscribe((list) => {
        if (list === null) {
          this.stats.update((current) => current.map((d) => ({ ...d, state: 'unknown' as const })));
          return;
        }
        this.unsyncedCount.set(list.unsyncedWriteBacks);
        this.stats.set(
          list.items.map((h) =>
            this.withName({
              device: h.device,
              kind: h.kind as DeviceKind,
              name: null,
              state: h.status === 'ready' ? 'ready' : 'offline',
            }),
          ),
        );
      });
  }

  stop(): void {
    this.polling?.unsubscribe();
    this.polling = null;
    this.stats.set([]);
    this.unsyncedCount.set(0);
  }

  private withName(stat: DeviceStat): DeviceStat {
    return stat.kind === 'scale'
      ? { ...stat, name: this.scaleNames.get(stat.device) ?? null }
      : stat;
  }
}
