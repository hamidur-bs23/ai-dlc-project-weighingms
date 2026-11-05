import { TestBed } from '@angular/core/testing';
import { httpMock, provideTestHttp, provideTestTimings } from '../../../testing/test-providers';
import { ScannerService } from './scanner.service';

const scansCall =
  (after: number) => (req: { url: string; params: { get(k: string): string | null } }) =>
    req.url === '/api/scanner/scans' && req.params.get('after') === String(after);

const scan = (sequence: number, code: string) => ({
  sequence,
  code,
  scannedAt: '2026-11-05T01:00:00Z',
});

describe('ScannerService', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [provideTestHttp(), provideTestTimings(5000, 500)],
    });
  });

  afterEach(() => {
    httpMock().verify();
    vi.useRealTimers();
  });

  it('should skip scans made before it started and deliver each later scan once, in order', () => {
    const codes: string[] = [];
    const sub = TestBed.inject(ScannerService)
      .scans()
      .subscribe((c) => codes.push(c));

    vi.advanceTimersByTime(0);
    // First poll: an old card scanned earlier must not open a batch now.
    httpMock()
      .expectOne(scansCall(0))
      .flush({ items: [scan(7, 'B-24190')], cursor: 7 });

    vi.advanceTimersByTime(500);
    httpMock()
      .expectOne(scansCall(7))
      .flush({ items: [scan(8, ' B-24187 '), scan(9, 'B-24188')], cursor: 9 });

    vi.advanceTimersByTime(500);
    httpMock().expectOne(scansCall(9)).flush({ items: [], cursor: 9 });

    expect(codes).toEqual(['B-24187', 'B-24188']);
    sub.unsubscribe();
  });

  it('should keep polling when the scanner is offline, and prime once it is back', () => {
    const codes: string[] = [];
    const sub = TestBed.inject(ScannerService)
      .scans()
      .subscribe((c) => codes.push(c));

    vi.advanceTimersByTime(0);
    httpMock()
      .expectOne(scansCall(0))
      .flush({ errorCode: 'Scanner.Unavailable' }, { status: 503, statusText: 'x' });

    vi.advanceTimersByTime(500);
    httpMock()
      .expectOne(scansCall(0))
      .flush({ items: [scan(3, 'B-24187')], cursor: 3 });

    vi.advanceTimersByTime(500);
    httpMock()
      .expectOne(scansCall(3))
      .flush({ items: [scan(4, 'B-24191')], cursor: 4 });

    expect(codes).toEqual(['B-24191']);
    sub.unsubscribe();
  });

  it('should ignore blank codes', () => {
    const codes: string[] = [];
    const sub = TestBed.inject(ScannerService)
      .scans()
      .subscribe((c) => codes.push(c));
    vi.advanceTimersByTime(0);
    httpMock().expectOne(scansCall(0)).flush({ items: [], cursor: 0 });
    vi.advanceTimersByTime(500);
    httpMock()
      .expectOne(scansCall(0))
      .flush({ items: [scan(1, '   ')], cursor: 1 });
    expect(codes).toEqual([]);
    sub.unsubscribe();
  });

  it('should stop polling when unsubscribed', () => {
    const sub = TestBed.inject(ScannerService).scans().subscribe();
    vi.advanceTimersByTime(0);
    httpMock().expectOne(scansCall(0)).flush({ items: [], cursor: 0 });
    sub.unsubscribe();
    vi.advanceTimersByTime(2000);
    httpMock().expectNone('/api/scanner/scans?after=0');
  });
});
