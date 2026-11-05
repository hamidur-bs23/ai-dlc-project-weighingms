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
    // The Weigh screen (bolt 010) is built on this route; for now it shows the opened batch.
    path: 'batches/:batchId/weigh',
    canActivate: [signedInGuard],
    loadComponent: () =>
      import('@features/batch-opened/batch-opened.component').then((m) => m.BatchOpenedComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'batches' },
  { path: '**', redirectTo: 'batches' },
];
