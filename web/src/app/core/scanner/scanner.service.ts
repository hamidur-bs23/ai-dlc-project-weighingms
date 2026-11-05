import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, concatMap, filter, from, map, of, timer } from 'rxjs';
import type { ScanList } from '@core/api/api.types';
import { STATION_UI_CONFIG } from '@core/station-config';

/**
 * Scans from the station scanner, through `GET /api/scanner/scans?after={cursor}`.
 *
 * Each subscriber starts at the scanner's *current* cursor: scans made before the screen opened
 * are skipped, so an old batch card can never open a batch later. Polls that fail (scanner
 * offline) are skipped; the status strip shows the outage.
 */
@Injectable({ providedIn: 'root' })
export class ScannerService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(STATION_UI_CONFIG);

  /** Codes scanned from now on, one emission per scan, in order. */
  scans(): Observable<string> {
    return new Observable<string>((subscriber) => {
      let cursor: number | null = null;

      const poll = timer(0, this.config.scannerPollMs)
        .pipe(
          concatMap(() =>
            this.http
              .get<ScanList>('/api/scanner/scans', { params: { after: cursor ?? 0 } })
              .pipe(catchError(() => of(null))),
          ),
          filter((list): list is ScanList => list !== null),
          concatMap((list) => {
            const primed = cursor !== null;
            cursor = list.cursor;
            // The first successful poll only finds the current cursor.
            return from(primed ? list.items : []);
          }),
          map((scan) => scan.code.trim()),
          filter((code) => code.length > 0),
        )
        .subscribe(subscriber);

      return () => {
        poll.unsubscribe();
      };
    });
  }
}
