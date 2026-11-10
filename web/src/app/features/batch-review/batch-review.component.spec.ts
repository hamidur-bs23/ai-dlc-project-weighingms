import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import type { CompletedBatch, CompletedBatchPage } from '@core/api/api.types';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
} from '../../../testing/test-providers';
import { BatchReviewComponent } from './batch-review.component';

const LIST_URL = (page: number) => `/api/weighing-sessions?status=completed&page=${page}`;

const completed: CompletedBatch = {
  batchId: 'B-24187',
  orderId: 'SO-9931',
  machine: 'JET-04',
  shade: 'Teal 4C',
  completedAt: '2026-11-05T01:48:22Z',
  done: 5,
  total: 5,
  allWithinTolerance: false,
  overrideCount: 2,
  operators: ['R. Karim', 'S. Akter'],
};

const pageOf = (page: number, total: number, items = [completed]): CompletedBatchPage => ({
  items,
  page,
  pageSize: 20,
  total,
});

describe('BatchReviewComponent (completed batches)', () => {
  beforeEach(async () => {
    await render(BatchReviewComponent, {
      providers: [provideTestHttp(), provideRouter([]), provideTestTransloco()],
    });
  });

  afterEach(() => {
    httpMock().verify();
  });

  it('should list each batch with its time, operators and override count', async () => {
    httpMock().expectOne(LIST_URL(1)).flush(pageOf(1, 1));

    const row = await screen.findByRole('row', { name: 'Review batch B-24187' });
    const cells = within(row)
      .getAllByRole('cell')
      .map((c) => c.textContent.trim());
    expect(cells).toEqual([
      'B-24187',
      'SO-9931',
      'JET-04',
      'Teal 4C',
      expect.stringMatching(/^2026-11-05 \d\d:48$/),
      'R. Karim, S. Akter',
      '2',
    ]);
  });

  it('should say so when nothing has been completed', async () => {
    httpMock()
      .expectOne(LIST_URL(1))
      .flush(pageOf(1, 0, []));

    expect(await screen.findByText('No batches have been completed yet.')).toBeTruthy();
  });

  it('should show no paging for one page', async () => {
    httpMock().expectOne(LIST_URL(1)).flush(pageOf(1, 20));

    await screen.findByRole('row', { name: 'Review batch B-24187' });
    expect(screen.queryByRole('button', { name: 'Next' })).toBeNull();
  });

  it('should move through the pages and stop at the ends', async () => {
    const user = userEvent.setup();
    httpMock().expectOne(LIST_URL(1)).flush(pageOf(1, 21));

    expect((await screen.findByTestId('review-page')).textContent.trim()).toBe('Page 1 of 2');
    expect(screen.getByRole<HTMLButtonElement>('button', { name: 'Previous' }).disabled).toBe(
      true,
    );

    await user.click(screen.getByRole('button', { name: 'Next' }));
    httpMock().expectOne(LIST_URL(2)).flush(pageOf(2, 21));

    await vi.waitFor(() => {
      expect(screen.getByTestId('review-page').textContent.trim()).toBe('Page 2 of 2');
    });
    await vi.waitFor(() => {
      expect(screen.getByRole<HTMLButtonElement>('button', { name: 'Next' }).disabled).toBe(
        true,
      );
    });

    await user.click(screen.getByRole('button', { name: 'Previous' }));
    httpMock().expectOne(LIST_URL(1)).flush(pageOf(1, 21));
    await vi.waitFor(() => {
      expect(screen.getByTestId('review-page').textContent.trim()).toBe('Page 1 of 2');
    });
  });

  it('should keep the page it was on when the next one fails, and retry the one that failed', async () => {
    const user = userEvent.setup();
    httpMock().expectOne(LIST_URL(1)).flush(pageOf(1, 21));
    await screen.findByTestId('review-page');

    await user.click(screen.getByRole('button', { name: 'Next' }));
    httpMock()
      .expectOne(LIST_URL(2))
      .flush(problem('Unexpected'), { status: 500, statusText: 'Error' });

    expect((await screen.findByTestId('review-page')).textContent.trim()).toBe('Page 1 of 2');

    await user.click(await screen.findByRole('button', { name: /Retry/ }));
    httpMock().expectOne(LIST_URL(2)).flush(pageOf(2, 21));

    await vi.waitFor(() => {
      expect(screen.getByTestId('review-page').textContent.trim()).toBe('Page 2 of 2');
    });
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('should show the error and still offer the way back when the list cannot load', async () => {
    httpMock()
      .expectOne(LIST_URL(1))
      .flush(problem('Unexpected'), { status: 500, statusText: 'Error' });

    expect(await screen.findByRole('alert')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Back to queue' })).toBeTruthy();
  });

  it.each([
    ['touch', async (row: HTMLElement) => userEvent.setup().click(row)],
    [
      'Enter',
      async (row: HTMLElement) => {
        row.focus();
        await userEvent.setup().keyboard('{Enter}');
      },
    ],
    [
      'Space',
      async (row: HTMLElement) => {
        row.focus();
        await userEvent.setup().keyboard(' ');
      },
    ],
  ])('should open a batch read-only by %s', async (_how, act) => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    httpMock().expectOne(LIST_URL(1)).flush(pageOf(1, 1));

    await act(await screen.findByRole('row', { name: 'Review batch B-24187' }));

    expect(navigate).toHaveBeenCalledWith(['/review', 'B-24187']);
  });

  it('should go back to the queue', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    httpMock().expectOne(LIST_URL(1)).flush(pageOf(1, 1));

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Back to queue' }));

    expect(navigate).toHaveBeenCalledWith(['/batches']);
  });
});
