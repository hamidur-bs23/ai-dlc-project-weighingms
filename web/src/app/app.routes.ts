import { Routes } from '@angular/router';
import { signedInGuard, signedOutGuard } from '@core/auth/auth.guards';

export const routes: Routes = [
  {
    path: 'sign-in',
    canActivate: [signedOutGuard],
    loadComponent: () =>
      import('@features/sign-in/sign-in.component').then((m) => m.SignInComponent),
  },
  {
    path: 'batches',
    canActivate: [signedInGuard],
    loadComponent: () =>
      import('@features/batch-queue/batch-queue.component').then((m) => m.BatchQueueComponent),
  },
  {
    // Screen 2 · Weigh (bolt 010).
    path: 'batches/:batchId/weigh',
    canActivate: [signedInGuard],
    loadComponent: () => import('@features/weigh/weigh.component').then((m) => m.WeighComponent),
  },
  {
    // The interim batch summary after completion (INTERIM-SUMMARY); bolt 013 makes it Confirm & print.
    path: 'batches/:batchId/summary',
    canActivate: [signedInGuard],
    loadComponent: () =>
      import('@features/batch-summary/batch-summary.component').then(
        (m) => m.BatchSummaryComponent,
      ),
  },
  { path: '', pathMatch: 'full', redirectTo: 'batches' },
  { path: '**', redirectTo: 'batches' },
];
