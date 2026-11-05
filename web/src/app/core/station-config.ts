import { InjectionToken } from '@angular/core';

/** UI timing that belongs in configuration, not in components (coding-standards). */
export interface StationUiConfig {
  /** How often the status strip refreshes device health. */
  healthPollMs: number;
  /** How often the scanner is asked for new scans. */
  scannerPollMs: number;
}

export const STATION_UI_CONFIG = new InjectionToken<StationUiConfig>('STATION_UI_CONFIG', {
  providedIn: 'root',
  factory: () => ({ healthPollMs: 5000, scannerPollMs: 500 }),
});
