import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { b24187, recipe24187 } from '../../../testing/fixtures';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
  settle,
} from '../../../testing/test-providers';
import { BatchOpenedComponent } from './batch-opened.component';

/** Renders the view; `opened` means the queue already opened the batch (its header is known). */
const setup = async (opened: boolean) => {
  const active = new ActiveBatchService();
  if (opened) {
    active.open(b24187);
  }
  await render(BatchOpenedComponent, {
    providers: [
      provideTestHttp(),
      provideRouter([]),
      provideTestTransloco(),
      { provide: ActiveBatchService, useValue: active },
    ],
    inputs: { batchId: 'B-24187' },
  });
};

describe('BatchOpenedComponent', () => {
  afterEach(() => {
    httpMock().verify();
  });

  it('should load only the recipe when opened from the queue (the STEP 19 third call)', async () => {
    await setup(true);
    httpMock().expectNone('/api/batches/B-24187');
    httpMock().expectOne('/api/batches/B-24187/recipe').flush(recipe24187);

    expect(await screen.findByRole('heading', { name: 'Recipe · Teal 4C' })).toBeTruthy();
    const rows = screen.getAllByRole('row').slice(1);
    expect(rows.map((r) => within(r).getAllByRole('cell')[2].textContent.trim())).toEqual([
      'Remazol Brill. Blue RSPL',
      'Remazol Red RB',
      'Remazol Yellow RGB',
      'Sodium Sulphate',
      'Soda Ash',
    ]);
    const yellow = within(rows[2])
      .getAllByRole('cell')
      .map((c) => c.textContent.trim());
    expect(yellow).toEqual(['3', 'DYE-4471', 'Remazol Yellow RGB', '94.0 g', '± 0.5 g', 'C-12']);
    const soda = within(rows[4])
      .getAllByRole('cell')
      .map((c) => c.textContent.trim());
    expect(soda.slice(3)).toEqual(['9.600 kg', 'configured', '—']);
  });

  it('should fetch the batch header first after a reload', async () => {
    await setup(false);
    httpMock().expectOne('/api/batches/B-24187').flush(b24187);
    await settle();
    httpMock().expectOne('/api/batches/B-24187/recipe').flush(recipe24187);
    expect(await screen.findByRole('heading', { name: 'Recipe · Teal 4C' })).toBeTruthy();
    expect(TestBed.inject(ActiveBatchService).batch()?.machine).toBe('JET-04');
  });

  it('should offer Retry when OrgaTEX is down for a batch not yet snapshotted', async () => {
    await setup(true);
    httpMock()
      .expectOne('/api/batches/B-24187/recipe')
      .flush(problem('OrgaTex.Unavailable'), { status: 503, statusText: 'x' });

    const alert = await screen.findByRole('alert');
    await userEvent.setup().click(within(alert).getByRole('button', { name: 'Retry' }));
    httpMock().expectOne('/api/batches/B-24187/recipe').flush(recipe24187);
    expect(await screen.findByRole('heading', { name: 'Recipe · Teal 4C' })).toBeTruthy();
  });

  it('should not offer Retry for a batch that does not exist', async () => {
    await setup(false);
    httpMock()
      .expectOne('/api/batches/B-24187')
      .flush(problem('Batch.NotFound'), { status: 404, statusText: 'x' });
    const alert = await screen.findByRole('alert');
    expect(alert.textContent).toContain('Batch not found.');
    expect(within(alert).queryByRole('button', { name: 'Retry' })).toBeNull();
  });

  it('should go back to the queue and clear the open batch', async () => {
    await setup(true);
    httpMock().expectOne('/api/batches/B-24187/recipe').flush(recipe24187);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    await userEvent.setup().click(await screen.findByRole('button', { name: 'Back to queue' }));
    expect(navigate).toHaveBeenCalledWith(['/batches']);
    expect(TestBed.inject(ActiveBatchService).batch()).toBeNull();
  });
});
