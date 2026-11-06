// The seeded training scenario as the Api returns it (specs only; the app never holds this data).
import type {
  Batch,
  DeviceHealthList,
  Recipe,
  ScaleList,
  Session,
  Station,
  WeighingComponent,
  WeighingReading,
  WeighingSession,
} from '@core/api/api.types';

export const operatorSession: Session = {
  badgeId: 'OP-1001',
  displayName: 'R. Karim',
  role: 'operator',
  shift: 'A',
  stationId: 'STATION 01',
  timeZone: 'Asia/Dhaka',
  signedInAt: '2026-11-05T01:00:00+00:00',
};

export const b24187: Batch = {
  batchId: 'B-24187',
  orderId: 'SO-9931',
  machine: 'JET-04',
  fabric: '100% CTN knit',
  batchWeight: { value: 480, unit: 'kg' },
  shade: 'Teal 4C',
  componentCount: 5,
  status: 'weighing',
  trainingOnly: false,
};

export const queue: Batch[] = [
  b24187,
  {
    ...b24187,
    batchId: 'B-24188',
    machine: 'JET-07',
    batchWeight: { value: 520, unit: 'kg' },
    status: 'queued',
    trainingOnly: true,
  },
  {
    batchId: 'B-24190',
    orderId: 'SO-9944',
    machine: 'JET-02',
    fabric: 'CVC fleece',
    batchWeight: { value: 310, unit: 'kg' },
    shade: 'Navy 12',
    componentCount: 7,
    status: 'queued',
    trainingOnly: true,
  },
  {
    batchId: 'B-24191',
    orderId: 'SO-9947',
    machine: 'WINCH-01',
    fabric: 'Viscose jersey',
    batchWeight: { value: 190, unit: 'kg' },
    shade: 'Rose 08',
    componentCount: 4,
    status: 'queued',
    trainingOnly: true,
  },
];

export const recipe24187: Recipe = {
  batchId: 'B-24187',
  shade: 'Teal 4C',
  loadedAt: '2026-11-05T01:12:30+00:00',
  trainingOnly: false,
  components: [
    {
      sequence: 1,
      chemicalId: 'DYE-1180',
      code: 'DYE-1180',
      name: 'Remazol Brill. Blue RSPL',
      displayColour: '#1f5fa8',
      target: { value: 1240, unit: 'g' },
      tolerance: { value: 6, unit: 'g' },
      suggestedContainer: 'C-09',
    },
    {
      sequence: 2,
      chemicalId: 'DYE-2204',
      code: 'DYE-2204',
      name: 'Remazol Red RB',
      displayColour: '#b3262e',
      target: { value: 386, unit: 'g' },
      tolerance: { value: 2, unit: 'g' },
      suggestedContainer: 'C-11',
    },
    {
      sequence: 3,
      chemicalId: 'DYE-4471',
      code: 'DYE-4471',
      name: 'Remazol Yellow RGB',
      displayColour: '#e3b51d',
      target: { value: 94, unit: 'g' },
      tolerance: { value: 0.5, unit: 'g' },
      suggestedContainer: 'C-12',
    },
    {
      sequence: 4,
      chemicalId: 'AUX-0101',
      code: 'AUX-0101',
      name: 'Sodium Sulphate',
      displayColour: '#cccccc',
      target: { value: 24, unit: 'kg' },
      tolerance: { value: 0.12, unit: 'kg' },
      suggestedContainer: 'D-03',
    },
    {
      sequence: 5,
      chemicalId: 'AUX-0107',
      code: 'AUX-0107',
      name: 'Soda Ash',
      displayColour: '#eeeeee',
      target: { value: 9.6, unit: 'kg' },
      tolerance: null,
      suggestedContainer: null,
    },
  ],
};

export const station: Station = {
  stationId: 'STATION 01',
  timeZone: 'Asia/Dhaka',
  utcNow: '2026-11-05T01:47:03+00:00',
  plantNow: '2026-11-05T07:47:03+06:00',
};

export const scales: ScaleList = {
  pollIntervalMs: 250,
  items: [
    {
      scaleId: 'scale-3kg',
      name: '3 kg precision',
      scaleClass: 'precision',
      capacity: { value: 3200, unit: 'g' },
      resolution: { value: 0.1, unit: 'g' },
      status: 'ready',
    },
    {
      scaleId: 'scale-32kg',
      name: '32 kg floor',
      scaleClass: 'heavyDuty',
      capacity: { value: 32, unit: 'kg' },
      resolution: { value: 0.001, unit: 'kg' },
      status: 'ready',
    },
  ],
};

export function health(overrides: Record<string, 'ready' | 'offline'> = {}): DeviceHealthList {
  const since = '2026-11-05T01:00:00+00:00';
  const item = (device: string, kind: string) => ({
    device,
    kind,
    status: overrides[device] ?? 'ready',
    since,
  });
  return {
    items: [
      item('scale-3kg', 'scale'),
      item('scale-32kg', 'scale'),
      item('scanner', 'scanner'),
      item('printer', 'printer'),
      item('orgatex', 'orgatex'),
    ],
  };
}

// ── Weighing (bolt 010) ─────────────────────────────────────────────────────────

const q = (value: number, unit: 'g' | 'kg') => ({ value, unit });

function component(
  sequence: number,
  code: string,
  name: string,
  target: number,
  unit: 'g' | 'kg',
  tol: number,
  container: string,
): WeighingComponent {
  const precision = unit === 'g';
  return {
    sequence,
    chemicalId: code,
    code,
    name,
    target: q(target, unit),
    state: 'pending',
    scale: {
      scaleId: precision ? 'scale-3kg' : 'scale-32kg',
      scaleClass: precision ? 'precision' : 'heavyDuty',
      unit,
      resolution: q(precision ? 0.1 : 0.001, unit),
      automatic: true,
    },
    tolerance: {
      status: 'resolved',
      source: 'recipe',
      plusMinus: q(tol, unit),
      lower: q(target - tol, unit),
      upper: q(target + tol, unit),
    },
    suggestedContainer: container,
    attempt: null,
    record: null,
  };
}

/** B-24187 as the Api returns its session; `active` makes one component active with an empty attempt. */
export function weighingSession(
  active: number | null = 3,
  overrides: Partial<WeighingSession> = {},
): WeighingSession {
  const components = [
    component(1, 'DYE-1180', 'Remazol Brill. Blue RSPL', 1240, 'g', 6, 'C-09'),
    component(2, 'DYE-2204', 'Remazol Red RB', 386, 'g', 2, 'C-11'),
    component(3, 'DYE-4471', 'Remazol Yellow RGB', 94, 'g', 0.5, 'C-12'),
    component(4, 'AUX-0101', 'Sodium Sulphate', 24, 'kg', 0.12, 'D-03'),
    component(5, 'AUX-0107', 'Soda Ash', 9.6, 'kg', 0.048, 'D-05'),
  ].map((c) =>
    c.sequence === active
      ? {
          ...c,
          state: 'active',
          attempt: { lot: null, container: null, tare: null, taredAt: null },
        }
      : c,
  );
  return {
    batchId: 'B-24187',
    status: 'inProgress',
    startedAt: '2026-11-05T01:41:41Z',
    startedBy: 'OP-1001',
    completedAt: null,
    completedBy: null,
    progress: { done: 0, total: 5 },
    suggestedSequence: 1,
    activeSequence: active,
    readyToComplete: false,
    components,
    ...overrides,
  };
}

/** The same session with the active component's attempt filled in. */
export function withAttempt(
  session: WeighingSession,
  attempt: { lot?: string | null; container?: string | null; tare?: number | null },
): WeighingSession {
  return {
    ...session,
    components: session.components.map((c) =>
      c.sequence === session.activeSequence
        ? {
            ...c,
            attempt: {
              lot: attempt.lot ?? null,
              container: attempt.container ?? null,
              tare: attempt.tare != null ? q(attempt.tare, c.target.unit as 'g' | 'kg') : null,
              taredAt: attempt.tare != null ? '2026-11-05T01:42:00Z' : null,
            },
          }
        : c,
    ),
  };
}

type ReadingClass = 'underweight' | 'withinRange' | 'targetAchieved' | 'overweight' | null;

/** A Yellow RGB reading (94.0 ± 0.5 g) as the server classifies it. */
export function yellowReading(
  net: number,
  cls: ReadingClass,
  options: {
    stable?: boolean;
    canConfirm?: boolean;
    missing?: string[];
    surplus?: number;
    offered?: boolean;
  } = {},
): WeighingReading {
  const stable = options.stable ?? true;
  const confirmable = cls === 'withinRange' || cls === 'targetAchieved';
  const missing = options.missing ?? [
    ...(stable ? [] : ['stable']),
    ...(confirmable ? [] : ['inTolerance']),
  ];
  return {
    sequence: 3,
    scaleId: 'scale-3kg',
    net: q(net, 'g'),
    stable,
    validity: 'valid',
    readAt: '2026-11-05T01:43:00Z',
    class: cls,
    remaining: q(Math.max(0, Math.round((94 - Math.max(0, net)) * 10) / 10), 'g'),
    band: { target: q(94, 'g'), plusMinus: q(0.5, 'g'), lower: q(93.5, 'g'), upper: q(94.5, 'g') },
    toleranceStatus: 'resolved',
    confirmation: { canConfirm: options.canConfirm ?? missing.length === 0, missing },
    surplus:
      options.surplus != null
        ? {
            amount: q(options.surplus, 'g'),
            percentOfTarget: Math.round((options.surplus / 94) * 1000) / 10,
          }
        : null,
    override: {
      offered:
        options.offered ??
        ((cls === 'underweight' || cls === 'overweight') &&
          missing.every((m) => m === 'inTolerance')),
    },
  };
}
