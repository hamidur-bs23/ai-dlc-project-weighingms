import { HttpClient } from '@angular/common/http';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Station } from '@core/api/api.types';

/**
 * The plant-time clock in the status strip (FR-22). The Api is the time authority: the offset
 * between its UTC clock and the terminal's is measured once, then the clock ticks locally and
 * is shown in the plant time zone from `GET /api/station`.
 */
@Injectable({ providedIn: 'root' })
export class PlantClockService {
  private readonly http = inject(HttpClient);
  private readonly station = signal<Station | null>(null);
  private readonly skewMs = signal(0);
  private readonly nowMs = signal(Date.now());
  private ticker: ReturnType<typeof setInterval> | null = null;

  readonly stationId = computed(() => this.station()?.stationId ?? null);

  /** The plant time zone (IANA id), or null until the station is known. */
  readonly timeZone = computed(() => this.station()?.timeZone ?? null);

  /** Plant time as HH:mm:ss, or null until the station is known. */
  readonly time = computed(() =>
    this.timeZone() === null ? null : this.format(this.nowMs() + this.skewMs(), true),
  );

  /**
   * Formats an instant as plant wall-clock time (HH:mm or HH:mm:ss). Before the station is known
   * it falls back to the terminal's zone, which is the same zone on a station terminal.
   */
  format(instant: Date | string | number, withSeconds = false): string {
    return new Intl.DateTimeFormat('en-GB', {
      timeZone: this.timeZone() ?? undefined,
      hour: '2-digit',
      minute: '2-digit',
      ...(withSeconds ? { second: '2-digit' } : {}),
      hourCycle: 'h23',
    }).format(new Date(instant));
  }

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.stop();
    });
  }

  async start(): Promise<void> {
    this.stop();
    try {
      const station = await firstValueFrom(this.http.get<Station>('/api/station'));
      this.skewMs.set(Date.parse(station.utcNow) - Date.now());
      this.station.set(station);
    } catch {
      this.station.set(null); // the strip shows no clock rather than a wrong one
      return;
    }
    this.nowMs.set(Date.now());
    this.ticker = setInterval(() => {
      this.nowMs.set(Date.now());
    }, 1000);
  }

  stop(): void {
    if (this.ticker !== null) {
      clearInterval(this.ticker);
      this.ticker = null;
    }
  }
}
