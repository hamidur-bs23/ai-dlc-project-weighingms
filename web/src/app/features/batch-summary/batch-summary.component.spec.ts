import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import type { BatchSummary } from '@core/api/api.types';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
} from '../../../testing/test-providers';
import { BatchSummaryComponent } from './batch-summary.component';

const summary: BatchSummary = {
  batchId: 'B-24187',
  orderId: 'SO-9931',
  machine: 'JET-04',
  shade: 'Teal 4C',
  status: 'completed',
  done: 5,
  total: 5,
  allWithinTolerance: false,
  overrideCount: 1,
  totalDispensed: { value: 35.322, unit: 'kg' },
  startedAt: '2026-11-05T01:41:41Z',
  completedAt: '2026-11-05T01:48:22Z',
  elapsedSeconds: 401,
  operators: ['R. Karim', 'S. Akter'],
  orgatex: { state: 'notAvailable' },
  ticket: { state: 'notAvailable' },
  lines: [
    {
      sequence: 1,
      chemicalId: 'DYE-1180',
      chemicalName: 'Remazol Brill. Blue RSPL',
      required: { value: 1240, unit: 'g' },
      actual: { value: 1241.5, unit: 'g' },
      status: 'withinRange',
      override: false,
    },
    {
      sequence: 2,
      chemicalId: 'DYE-2204',
      chemicalName: 'Remazol Red RB',
      required: { value: 386, unit: 'g' },
      actual: { value: 389.1, unit: 'g' },
      status: 'overweight',
      override: true,
    },
  ],
};

describe('BatchSummaryComponent (interim summary)', () => {
  beforeEach(async () => {
    await render(BatchSummaryComponent, {
      providers: [provideTestHttp(), provideRouter([]), provideTestTransloco()],
      inputs: { batchId: 'B-24187' },
    });
  });

  afterEach(() => {
    httpMock().verify();
  });

  it('should show the server summary read-only', async () => {
    httpMock().expectOne('/api/batches/B-24187/session/summary').flush(summary);

    expect(await screen.findByRole('heading', { name: 'Batch B-24187 weighed' })).toBeTruthy();
    expect(screen.getByTestId('summary-weighed').textContent.trim()).toBe('5 of 5');
    expect(screen.getByText('NO · 1 override(s)')).toBeTruthy();
    expect(screen.getByText('35.322 kg')).toBeTruthy();
    expect(screen.getByText('6 min 41 s')).toBeTruthy();
    expect(screen.getByText('R. Karim, S. Akter')).toBeTruthy();
    const rows = screen.getAllByRole('row').slice(1);
    expect(within(rows[1]).getByText('OVR')).toBeTruthy();
    expect(within(rows[1]).getByText('Overweight', { exact: false })).toBeTruthy();
    expect(screen.queryByRole('textbox')).toBeNull();
  });

  it('should offer Retry when the summary cannot be loaded', async () => {
    httpMock()
      .expectOne('/api/batches/B-24187/session/summary')
      .flush(problem('Weighing.SessionNotFound'), { status: 404, statusText: 'x' });

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Retry' }));
    httpMock().expectOne('/api/batches/B-24187/session/summary').flush(summary);
    expect(await screen.findByText('35.322 kg')).toBeTruthy();
  });

  it('should go back to the queue', async () => {
    httpMock().expectOne('/api/batches/B-24187/session/summary').flush(summary);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Back to queue' }));

    expect(navigate).toHaveBeenCalledWith(['/batches']);
  });
});
