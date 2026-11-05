import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import type { Batch, BatchList, Recipe } from '@core/api/api.types';

/** The batch endpoints of WeighingMS.Api (workbook STEP 19 paths). */
@Injectable({ providedIn: 'root' })
export class BatchesApiService {
  private readonly http = inject(HttpClient);

  list(): Observable<BatchList> {
    return this.http.get<BatchList>('/api/batches');
  }

  get(batchId: string): Observable<Batch> {
    return this.http.get<Batch>(`/api/batches/${encodeURIComponent(batchId)}`);
  }

  recipe(batchId: string): Observable<Recipe> {
    return this.http.get<Recipe>(`/api/batches/${encodeURIComponent(batchId)}/recipe`);
  }
}
