import { computed, inject } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { Subscription, catchError, exhaustMap, firstValueFrom, of, timer } from 'rxjs';
import type {
  Recipe,
  WeighingComponent,
  WeighingReading,
  WeighingSession,
} from '@core/api/api.types';
import { BatchesApiService } from '@core/batches/batches-api.service';
import { AppError, toAppError } from '@core/errors/app-error';
import { WeighingApiService } from '@core/weighing/weighing-api.service';

interface WeighingState {
  batchId: string | null;
  session: WeighingSession | null;
  recipe: Recipe | null;
  scaleNames: Readonly<Record<string, string>>;
  pollMs: number;
  reading: WeighingReading | null;
  readingError: AppError | null;
  commandError: AppError | null;
  loadError: AppError | null;
  busy: boolean;
}

const initialState: WeighingState = {
  batchId: null,
  session: null,
  recipe: null,
  scaleNames: {},
  pollMs: 250,
  reading: null,
  readingError: null,
  commandError: null,
  loadError: null,
  busy: false,
};

/**
 * The weighing session of the Weigh screen (tech-stack: NgRx SignalStore for the weighing session). It mirrors the
 * server's session and the live reading of the active component, polled at the server's interval. It decides nothing:
 * class, can-confirm, missing and override offer all come from the Api.
 */
export const WeighingSessionStore = signalStore(
  withState(initialState),
  withComputed(({ session, reading, busy, recipe }) => ({
    active: computed<WeighingComponent | null>(() => {
      const s = session();
      return s?.components.find((c) => c.sequence === s.activeSequence) ?? null;
    }),
    completed: computed(() => session()?.status === 'completed'),
    /** The reading, only if it belongs to the active component. */
    activeReading: computed<WeighingReading | null>(() => {
      const r = reading();
      return r !== null && r.sequence === session()?.activeSequence ? r : null;
    }),
    colourOf: computed(() => {
      const colours = new Map(
        (recipe()?.components ?? []).map((c) => [c.sequence, c.displayColour]),
      );
      return (sequence: number): string | null => colours.get(sequence) ?? null;
    }),
    isBusy: computed(() => busy()),
  })),
  withComputed(({ activeReading, isBusy }) => ({
    canConfirm: computed(() => !isBusy() && activeReading()?.confirmation.canConfirm === true),
    overrideOffered: computed(() => !isBusy() && activeReading()?.override.offered === true),
  })),
  withMethods((store, api = inject(WeighingApiService), batches = inject(BatchesApiService)) => {
    let polling: Subscription | null = null;

    const stopPolling = (): void => {
      polling?.unsubscribe();
      polling = null;
    };

    const startPolling = (): void => {
      stopPolling();
      const batchId = store.batchId();
      if (batchId === null || store.active() === null || store.completed()) {
        patchState(store, { reading: null });
        return;
      }
      polling = timer(0, store.pollMs())
        .pipe(
          exhaustMap(() =>
            api.reading(batchId).pipe(
              catchError((e: unknown) => {
                patchState(store, { readingError: toAppError(e) });
                return of(null);
              }),
            ),
          ),
        )
        .subscribe((reading) => {
          if (reading !== null) {
            patchState(store, { reading, readingError: null });
          }
        });
    };

    const applySession = (session: WeighingSession): void => {
      const activeBefore = store.session()?.activeSequence ?? null;
      patchState(store, { session, commandError: null });
      if (session.activeSequence !== activeBefore || polling === null) {
        patchState(store, { reading: null });
        startPolling();
      }
    };

    // Commands run one after another, never dropped: leaving the container field saves it while the operator is
    // already typing the lot, and both must reach the server in order.
    let queue: Promise<unknown> = Promise.resolve();
    let pending = 0;

    /** Queues one command; the screen is busy while any is pending (F1 is disabled), errors go to the banner. */
    const command = (call: () => Promise<WeighingSession>): Promise<boolean> => {
      pending++;
      patchState(store, { busy: true });
      const run = queue.then(async () => {
        try {
          applySession(await call());
          return true;
        } catch (e) {
          patchState(store, { commandError: toAppError(e) });
          return false;
        } finally {
          pending--;
          patchState(store, { busy: pending > 0 });
        }
      });
      queue = run;
      return run;
    };

    const id = (): string => store.batchId() ?? '';

    return {
      /** Opens the batch: scale names and poll interval, the recipe (STEP 19), then start or resume the session. */
      async open(batchId: string): Promise<void> {
        stopPolling();
        patchState(store, { ...initialState, batchId });
        try {
          const [scales, recipe, session] = await Promise.all([
            firstValueFrom(api.scales()),
            firstValueFrom(batches.recipe(batchId)),
            firstValueFrom(api.open(batchId)),
          ]);
          patchState(store, {
            scaleNames: Object.fromEntries(scales.items.map((s) => [s.scaleId, s.name])),
            pollMs: scales.pollIntervalMs,
            recipe,
          });
          // A fresh or resumed session with nothing active starts on the suggested component (PD-07).
          if (
            session.activeSequence == null &&
            session.suggestedSequence != null &&
            session.status !== 'completed'
          ) {
            applySession(await firstValueFrom(api.select(batchId, session.suggestedSequence)));
          } else {
            applySession(session);
          }
        } catch (e) {
          patchState(store, { loadError: toAppError(e) });
        }
      },
      select: (sequence: number) => command(() => firstValueFrom(api.select(id(), sequence))),
      captureLot: (lot: string) =>
        command(() => firstValueFrom(api.captureLot(id(), store.active()?.sequence ?? 0, lot))),
      captureContainer: (container: string) =>
        command(() =>
          firstValueFrom(api.captureContainer(id(), store.active()?.sequence ?? 0, container)),
        ),
      tare: () => command(() => firstValueFrom(api.tare(id(), store.active()?.sequence ?? 0))),
      quit: () => command(() => firstValueFrom(api.quit(id(), store.active()?.sequence ?? 0))),
      leave: () => command(() => firstValueFrom(api.leave(id()))),
      confirm: () =>
        command(
          async () =>
            (await firstValueFrom(api.confirm(id(), store.active()?.sequence ?? 0))).session,
        ),
      /** The override after the step-up; errors are returned to the dialog, not the banner. */
      async override(grantId: string, reason: string): Promise<AppError | null> {
        const sequence = store.active()?.sequence ?? 0;
        patchState(store, { busy: true });
        try {
          const result = await firstValueFrom(api.override(id(), sequence, grantId, reason));
          applySession(result.session);
          return null;
        } catch (e) {
          return toAppError(e);
        } finally {
          patchState(store, { busy: false });
        }
      },
      clearCommandError(): void {
        patchState(store, { commandError: null });
      },
      stop: stopPolling,
    };
  }),
  withHooks({
    onDestroy(store) {
      store.stop();
    },
  }),
);

export type WeighingSessionStoreType = InstanceType<typeof WeighingSessionStore>;
