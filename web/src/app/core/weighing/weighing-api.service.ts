import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import type {
  BatchSummary,
  CompletedBatchPage,
  ConfirmationResult,
  OrgatexState,
  ScaleList,
  StepUpGrant,
  Ticket,
  WeighingReading,
  WeighingSession,
} from '@core/api/api.types';

/** The weighing-session endpoints of WeighingMS.Api (bolts 007–009). Every rule is decided on the server. */
@Injectable({ providedIn: 'root' })
export class WeighingApiService {
  private readonly http = inject(HttpClient);

  private url(batchId: string, path = ''): string {
    return `/api/batches/${encodeURIComponent(batchId)}/session${path}`;
  }

  open(batchId: string): Observable<WeighingSession> {
    return this.http.post<WeighingSession>(this.url(batchId), null);
  }

  select(batchId: string, sequence: number): Observable<WeighingSession> {
    return this.http.post<WeighingSession>(this.url(batchId, '/selection'), { sequence });
  }

  captureLot(batchId: string, sequence: number, lot: string): Observable<WeighingSession> {
    return this.http.post<WeighingSession>(this.url(batchId, '/attempt/lot'), { sequence, lot });
  }

  captureContainer(
    batchId: string,
    sequence: number,
    container: string,
  ): Observable<WeighingSession> {
    return this.http.post<WeighingSession>(this.url(batchId, '/attempt/container'), {
      sequence,
      container,
    });
  }

  tare(batchId: string, sequence: number): Observable<WeighingSession> {
    return this.http.post<WeighingSession>(this.url(batchId, '/tare'), { sequence });
  }

  confirm(batchId: string, sequence: number): Observable<ConfirmationResult> {
    return this.http.post<ConfirmationResult>(this.url(batchId, '/confirmation'), { sequence });
  }

  override(
    batchId: string,
    sequence: number,
    grantId: string,
    reason: string,
  ): Observable<ConfirmationResult> {
    return this.http.post<ConfirmationResult>(this.url(batchId, '/override'), {
      sequence,
      grantId,
      reason,
    });
  }

  quit(batchId: string, sequence: number): Observable<WeighingSession> {
    return this.http.post<WeighingSession>(this.url(batchId, '/quit'), { sequence });
  }

  leave(batchId: string): Observable<WeighingSession> {
    return this.http.post<WeighingSession>(this.url(batchId, '/leave'), null);
  }

  reading(batchId: string): Observable<WeighingReading> {
    return this.http.get<WeighingReading>(this.url(batchId, '/reading'));
  }

  summary(batchId: string): Observable<BatchSummary> {
    return this.http.get<BatchSummary>(this.url(batchId, '/summary'));
  }

  /** Manual retry of a sync-failed OrgaTEX write-back (bolt 011). */
  retryWriteBack(batchId: string): Observable<OrgatexState> {
    return this.http.post<OrgatexState>(this.url(batchId, '/write-back/retry'), null);
  }

  /** The batch's frozen ticket: the same content as the printed ticket (bolt 012). */
  ticket(batchId: string): Observable<Ticket> {
    return this.http.get<Ticket>(this.url(batchId, '/ticket'));
  }

  /** Prints the ticket. `expectedPrintCount` is the count on screen, so a stale press is refused. */
  printTicket(batchId: string, expectedPrintCount: number): Observable<Ticket> {
    return this.http.post<Ticket>(this.url(batchId, '/ticket/print'), { expectedPrintCount });
  }

  /** One page of completed batches, newest first (REVIEW-LIST). */
  completedBatches(page: number): Observable<CompletedBatchPage> {
    return this.http.get<CompletedBatchPage>('/api/weighing-sessions', {
      params: { status: 'completed', page },
    });
  }

  scales(): Observable<ScaleList> {
    return this.http.get<ScaleList>('/api/scales');
  }

  /** Supervisor badge + PIN for one override (ADR-009, OVR-GRANT-FLOW). */
  stepUp(badgeId: string, pin: string): Observable<StepUpGrant> {
    return this.http.post<StepUpGrant>('/api/session/step-up', {
      badgeId,
      pin,
      action: 'override',
    });
  }
}
