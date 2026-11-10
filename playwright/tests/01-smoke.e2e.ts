import { expect, test } from '@playwright/test';

test.describe('the packaged app', () => {
  test('should serve the UI, including a route opened by URL', async ({ page }) => {
    const response = await page.goto('/batches');
    expect(response?.status()).toBe(200);
    // Not signed in yet, so the guard shows the sign-in screen instead of the queue.
    await expect(page.getByRole('heading', { name: 'Sign in to the station' })).toBeVisible();
  });

  test('should report ready, and keep an unknown API path a 404', async ({ request }) => {
    expect((await request.get('/health/ready')).status()).toBe(200);
    const unknown = await request.get('/api/no-such-thing');
    expect(unknown.status()).toBe(404);
    expect(unknown.headers()['content-type'] ?? '').not.toContain('text/html');
  });
});
