using Microsoft.Extensions.Options;
using WeighingMS.Simulators.Host.Common;
using WeighingMS.Simulators.Host.Seed;

namespace WeighingMS.Simulators.Host.Scales;

/// <summary>
/// Simulates the station scales. Gross = container tare + poured amount; net = gross − stored tare.
/// The poured amount follows the active scenario over time (deterministic through <see cref="TimeProvider"/>),
/// fast at first and easing off towards the landing value. All arithmetic is <see cref="decimal"/>.
/// </summary>
public sealed class ScaleSimulator
{
    /// <summary>Status reported by a working scale.</summary>
    public const string Ready = "READY";

    private readonly TimeProvider _clock;
    private readonly TimeSpan _pourDuration;
    private readonly Dictionary<string, ScaleState> _states;

    /// <summary>Creates the simulator from the seed scales.</summary>
    public ScaleSimulator(SimulatorSeed seed, TimeProvider clock, IOptions<SimulatorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(options);
        _clock = clock;
        _pourDuration = options.Value.PourDuration;
        _states = seed.Scales.ToDictionary(s => s.ScaleId, s => new ScaleState(s), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>All scales in seed order.</summary>
    public IReadOnlyList<SeedScale> Scales => [.. _states.Values.Select(s => s.Definition)];

    /// <summary>Finds a scale definition, or null.</summary>
    public SeedScale? Find(string scaleId) => _states.TryGetValue(scaleId, out var s) ? s.Definition : null;

    /// <summary>Current net reading of a scale.</summary>
    public WeightReadingResponse Read(string scaleId)
    {
        var state = _states[scaleId];
        lock (state)
        {
            var now = _clock.GetUtcNow();
            var (poured, stable) = state.PouredAt(now, _pourDuration);
            var def = state.Definition;
            var net = MassUnits.RoundToResolution(def.ContainerTare + poured - state.Tare, def.Resolution);
            var overCapacity = net > def.Capacity;
            return new WeightReadingResponse(def.ScaleId, overCapacity ? def.Capacity : net, def.Unit, stable, overCapacity, now);
        }
    }

    /// <summary>Tares the scale: the current gross becomes the stored tare, so the net reading is zero.</summary>
    public TareResponse Tare(string scaleId)
    {
        var state = _states[scaleId];
        lock (state)
        {
            var now = _clock.GetUtcNow();
            var (poured, _) = state.PouredAt(now, _pourDuration);
            var def = state.Definition;
            state.Tare = MassUnits.RoundToResolution(def.ContainerTare + poured, def.Resolution);
            return new TareResponse(def.ScaleId, state.Tare, def.Unit, now);
        }
    }

    /// <summary>
    /// Starts a scenario. Returns an error message when the request is invalid for the scenario, otherwise null.
    /// </summary>
    public string? SetScenario(string scaleId, ScaleScenario scenario, QuantityDto? target, decimal? tolerance)
    {
        var state = _states[scaleId];
        var def = state.Definition;
        lock (state)
        {
            var now = _clock.GetUtcNow();
            if (scenario == ScaleScenario.Stable)
            {
                var (poured, _) = state.PouredAt(now, _pourDuration);
                state.Hold(poured);
                return null;
            }

            if (target is null || target.Value <= 0m || !MassUnits.IsKnown(target.Unit))
            {
                return "A positive target with unit g or kg is required.";
            }

            var needsTolerance = scenario is ScaleScenario.BelowRange or ScaleScenario.EnterRange or ScaleScenario.Overshoot;
            if (needsTolerance && (tolerance is null || tolerance <= 0m))
            {
                return $"A positive tolerance is required for the '{scenario}' scenario.";
            }

            var targetInScaleUnit = MassUnits.Convert(target.Value, target.Unit, def.Unit);
            var toleranceInScaleUnit = MassUnits.Convert(tolerance ?? 0m, target.Unit, def.Unit);
            var landing = scenario switch
            {
                ScaleScenario.BelowRange => targetInScaleUnit - (2m * toleranceInScaleUnit),
                ScaleScenario.EnterRange => targetInScaleUnit + (0.4m * toleranceInScaleUnit),
                ScaleScenario.Overshoot => targetInScaleUnit + (4.8m * toleranceInScaleUnit),
                _ => targetInScaleUnit,
            };
            state.StartPour(Math.Max(0m, landing), now);
            return null;
        }
    }

    private sealed class ScaleState(SeedScale definition)
    {
        private decimal _landing;
        private decimal _held;
        private DateTimeOffset? _pourStartedAt;

        public SeedScale Definition { get; } = definition;

        public decimal Tare { get; set; }

        public void StartPour(decimal landing, DateTimeOffset now)
        {
            _landing = landing;
            _held = 0m;
            _pourStartedAt = now;
        }

        public void Hold(decimal poured)
        {
            _held = poured;
            _pourStartedAt = null;
        }

        public (decimal Poured, bool Stable) PouredAt(DateTimeOffset now, TimeSpan duration)
        {
            if (_pourStartedAt is null)
            {
                return (_held, true);
            }

            var elapsed = now - _pourStartedAt.Value;
            if (elapsed >= duration)
            {
                return (_landing, true);
            }

            var progress = elapsed <= TimeSpan.Zero ? 0m : (decimal)elapsed.Ticks / duration.Ticks;
            var remaining = 1m - progress;
            var eased = 1m - (remaining * remaining * remaining);
            return (_landing * eased, false);
        }
    }
}
