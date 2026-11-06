import { signedInGuard } from '@core/auth/auth.guards';
import { routes } from './app.routes';

describe('routes', () => {
  const route = (path: string) => routes.find((r) => r.path === path);

  it('should open a batch read-only from the review list only (REVIEW-READ-ONLY)', () => {
    expect(route('review/:batchId')?.data).toEqual({ readOnly: true });
    expect(route('batches/:batchId/summary')?.data).toBeUndefined();
  });

  it.each(['review', 'review/:batchId', 'batches/:batchId/summary'])(
    'should require sign-in for %s',
    (path) => {
      expect(route(path)?.canActivate).toEqual([signedInGuard]);
    },
  );
});
