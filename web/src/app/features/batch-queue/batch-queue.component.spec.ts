import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { Subject } from 'rxjs';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { ScannerService } from '@core/scanner/scanner.service';
import { b24187, queue } from '../../../testing/fixtures';
import {
  httpMock,
  problem,
  provideTestHttp,
  provideTestTransloco,
} from '../../../testing/test-providers';
import { BatchQueueComponent } from './batch-queue.component';

describe('BatchQueueComponent (screen 1 · Select batch)', () => {
  let scans: Subject<string>;
  let navigate: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    scans = new Subject<string>();
    await render(BatchQueueComponent, {
      providers: [
        provideTestHttp(),
        provideRouter([]),
        provideTestTransloco(),
        { provide: ScannerService, useValue: { scans: () => scans.asObservable() } },
      ],
    });
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true) as never;
  });

  afterEach(() => {
    httpMock().verify();
  });

  const loadQueue = async (items = queue) => {
    httpMock().expectOne('/api/batches').flush({ items });
    if (items.length > 0) {
      await screen.findByRole('table');
    }
  };

  const bodyRows = () => screen.getAllByRole('row').slice(1);

  it('should tag completed batches with their OrgaTEX write-back status', async () => {
    await loadQueue([
      { ...b24187, batchId: 'B-1', status: 'complete' },
      { ...b24187, batchId: 'B-2', status: 'syncPending' },
      { ...b24187, batchId: 'B-3', status: 'syncFailed' },
      { ...b24187, batchId: 'B-4', status: 'printPending' },
    ]);

    const tags = bodyRows().map((r) =>
      within(r).getAllByRole('cell').at(-1)?.querySelector('.tag'),
    );
    expect(tags.map((t) => t?.textContent.trim())).toEqual([
      'COMPLETE',
      'SYNC PENDING',
      'SYNC FAILED',
      'PRINT PENDING',
    ]);
    expect(tags[0]?.classList.contains('tag--done')).toBe(true);
    expect(tags[1]?.classList.contains('tag--now')).toBe(true);
    expect(tags[2]?.classList.contains('tag--failed')).toBe(true);
    expect(tags[3]?.classList.contains('tag--now')).toBe(true);
  });

  it('should show the scan prompt and the queue from GET /api/batches with every column', async () => {
    expect(screen.getByText('Scan a batch card to begin')).toBeTruthy();
    await loadQueue();

    const headers = screen.getAllByRole('columnheader').map((h) => h.textContent.trim());
    expect(headers).toEqual([
      'Batch',
      'Order',
      'Machine',
      'Fabric / lot',
      'Shade',
      'Comp.',
      'Status',
    ]);

    const cells = within(bodyRows()[0])
      .getAllByRole('cell')
      .map((c) => c.textContent.trim());
    expect(cells).toEqual([
      'B-24187',
      'SO-9931',
      'JET-04',
      '100% CTN knit · 480 kg',
      'Teal 4C',
      '5',
      'WEIGHING',
    ]);
    expect(bodyRows().map((r) => within(r).getAllByRole('cell')[0].textContent.trim())).toEqual([
      'B-24187',
      'B-24188',
      'B-24190',
      'B-24191',
    ]);
  });

  it('should label every status in text, never by colour alone', async () => {
    await loadQueue();
    expect(within(bodyRows()[0]).getByText('WEIGHING')).toBeTruthy();
    expect(within(bodyRows()[1]).getByText('QUEUED')).toBeTruthy();
    expect(within(bodyRows()[1]).getByText('TRAINING')).toBeTruthy();
  });

  it('should show only what the Api sends: a batch removed upstream is not on screen', async () => {
    await loadQueue(queue.filter((b) => b.batchId !== 'B-24190'));
    expect(screen.queryByText('B-24190')).toBeNull();
    expect(bodyRows()).toHaveLength(3);
  });

  it('should open a touched batch after checking it with GET /api/batches/{id}', async () => {
    await loadQueue();
    await userEvent.setup().click(screen.getByRole('row', { name: 'Open batch B-24187' }));

    httpMock().expectOne('/api/batches/B-24187').flush(b24187);
    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/batches', 'B-24187', 'weigh']);
    });
    expect(TestBed.inject(ActiveBatchService).batch()?.orderId).toBe('SO-9931');
  });

  it('should open a batch from the keyboard too', async () => {
    await loadQueue();
    screen.getByRole('row', { name: 'Open batch B-24188' }).focus();
    await userEvent.setup().keyboard('{Enter}');
    httpMock()
      .expectOne('/api/batches/B-24188')
      .flush({ ...b24187, batchId: 'B-24188' });
    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/batches', 'B-24188', 'weigh']);
    });
  });

  it('should open a scanned batch ID', async () => {
    await loadQueue();
    scans.next('B-24187');
    httpMock().expectOne('/api/batches/B-24187').flush(b24187);
    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/batches', 'B-24187', 'weigh']);
    });
  });

  it('should show "not found" for an unknown scanned ID and stay on the queue', async () => {
    await loadQueue();
    scans.next('B-99999');
    httpMock()
      .expectOne('/api/batches/B-99999')
      .flush(problem('Batch.NotFound'), { status: 404, statusText: 'x' });

    const alert = await screen.findByRole('alert');
    expect(alert.textContent).toContain('Batch B-99999 not found.');
    expect(navigate).not.toHaveBeenCalled();
    expect(bodyRows()).toHaveLength(4);
  });

  it('should offer Retry when a batch cannot be opened because OrgaTEX is down', async () => {
    await loadQueue();
    scans.next('B-24187');
    httpMock()
      .expectOne('/api/batches/B-24187')
      .flush(problem('OrgaTex.Unavailable'), { status: 503, statusText: 'x' });

    const alert = await screen.findByRole('alert');
    expect(alert.textContent).toContain('OrgaTEX is not responding.');
    await userEvent.setup().click(within(alert).getByRole('button', { name: 'Retry' }));
    httpMock().expectOne('/api/batches/B-24187').flush(b24187);
    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledWith(['/batches', 'B-24187', 'weigh']);
    });
  });

  it('should ignore a second selection while one is opening', async () => {
    await loadQueue();
    scans.next('B-24187');
    scans.next('B-24188');
    httpMock().expectOne('/api/batches/B-24187').flush(b24187);
    httpMock().expectNone('/api/batches/B-24188');
    await vi.waitFor(() => {
      expect(navigate).toHaveBeenCalledOnce();
    });
  });

  it('should offer Retry when OrgaTEX is down, and load the queue once it is back', async () => {
    httpMock()
      .expectOne('/api/batches')
      .flush(problem('OrgaTex.Unavailable'), { status: 503, statusText: 'x' });

    const alert = await screen.findByRole('alert');
    expect(alert.textContent).toContain('OrgaTEX is not responding.');
    await userEvent.setup().click(within(alert).getByRole('button', { name: 'Retry' }));

    await loadQueue();
    expect(screen.queryByRole('alert')).toBeNull();
    expect(bodyRows()).toHaveLength(4);
  });

  it('should say so when no batches are released', async () => {
    await loadQueue([]);
    expect(await screen.findByText('No batches released for weighing.')).toBeTruthy();
    expect(screen.queryByRole('table')).toBeNull();
  });
});
