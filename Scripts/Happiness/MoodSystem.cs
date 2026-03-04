using System;
using OrbitalRings.Data;

namespace OrbitalRings.Happiness;

/// <summary>
/// Encapsulates all station mood math: exponential decay toward a rising baseline,
/// mood tier calculation with hysteresis, and wish-fulfillment gain.
///
/// POCO (plain C# class) — no Godot Node inheritance. Owned by HappinessManager,
/// which passes delta time from _Process. Designed for testability in isolation.
/// </summary>
public class MoodSystem
{
    private readonly HappinessConfig _config;
    private float _mood;
    private float _baseline;
    private MoodTier _currentTier = MoodTier.Quiet;

    public float Mood => _mood;
    public float Baseline => _baseline;
    public MoodTier CurrentTier => _currentTier;

    public MoodSystem(HappinessConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Called from HappinessManager._Process every frame. Recomputes baseline
    /// from lifetime wish count, applies exponential decay toward baseline,
    /// then recalculates tier with hysteresis.
    /// Returns the new tier (caller compares to previous to detect changes).
    /// </summary>
    public MoodTier Update(float delta, int lifetimeHappiness)
    {
        // Recompute baseline FIRST (so decay targets the updated floor this frame)
        _baseline = MathF.Min(
            _config.BaselineCap,
            _config.BaselineScalingFactor * MathF.Sqrt(lifetimeHappiness)
        );

        // Exponential smoothing: frame-rate independent decay toward baseline
        float alpha = 1f - MathF.Exp(-_config.DecayRate * delta);
        _mood = _mood + (_baseline - _mood) * alpha;

        // Clamp to avoid floating-point undershoot below baseline
        _mood = MathF.Max(_mood, _baseline);

        // Recalculate tier with hysteresis state machine
        _currentTier = CalculateTier(_mood, _currentTier);
        return _currentTier;
    }

    /// <summary>
    /// Called on wish fulfillment. Applies flat mood gain (no diminishing returns).
    /// Caps at 1.0. Returns new tier (caller checks for tier change).
    /// </summary>
    public MoodTier OnWishFulfilled()
    {
        _mood = MathF.Min(1.0f, _mood + _config.MoodGainPerWish);
        _currentTier = CalculateTier(_mood, _currentTier);
        return _currentTier;
    }

    /// <summary>
    /// Restores mood and baseline from saved values. Used by HappinessManager.RestoreState.
    /// Recalculates tier from restored mood to ensure consistency.
    /// </summary>
    public void RestoreState(float mood, float baseline)
    {
        _mood = Math.Clamp(mood, 0f, 1f);
        _baseline = Math.Clamp(baseline, 0f, _config.BaselineCap);
        // Recalculate tier from scratch on restore (no previous tier context)
        _currentTier = CalculateTierFromScratch(_mood);
    }

    /// <summary>
    /// Tier transition with hysteresis. Promotion uses exact threshold;
    /// demotion uses threshold - hysteresisWidth to prevent rapid oscillation.
    /// Only checks one tier step at a time (prevents multi-tier skipping in a single call).
    /// </summary>
    private MoodTier CalculateTier(float mood, MoodTier current)
    {
        // Promotion: one step up if mood has reached the exact upper threshold
        if (current < MoodTier.Radiant && mood >= PromoteThreshold(current))
            return current + 1;

        // Demotion: one step down if mood dropped below threshold minus hysteresis buffer
        if (current > MoodTier.Quiet && mood < DemoteThreshold(current))
            return current - 1;

        return current;
    }

    /// <summary>
    /// Full tier recalculation from scratch (no hysteresis — used on save restore only).
    /// Finds the highest tier whose promote threshold the mood meets.
    /// </summary>
    private MoodTier CalculateTierFromScratch(float mood)
    {
        if (mood >= _config.TierRadiantThreshold) return MoodTier.Radiant;
        if (mood >= _config.TierVibrantThreshold) return MoodTier.Vibrant;
        if (mood >= _config.TierLivelyThreshold) return MoodTier.Lively;
        if (mood >= _config.TierCozyThreshold) return MoodTier.Cozy;
        return MoodTier.Quiet;
    }

    private float PromoteThreshold(MoodTier tier) => tier switch
    {
        MoodTier.Quiet => _config.TierCozyThreshold,
        MoodTier.Cozy => _config.TierLivelyThreshold,
        MoodTier.Lively => _config.TierVibrantThreshold,
        MoodTier.Vibrant => _config.TierRadiantThreshold,
        _ => float.MaxValue   // Radiant cannot promote
    };

    private float DemoteThreshold(MoodTier tier) => tier switch
    {
        MoodTier.Cozy => _config.TierCozyThreshold - _config.HysteresisWidth,
        MoodTier.Lively => _config.TierLivelyThreshold - _config.HysteresisWidth,
        MoodTier.Vibrant => _config.TierVibrantThreshold - _config.HysteresisWidth,
        MoodTier.Radiant => _config.TierRadiantThreshold - _config.HysteresisWidth,
        _ => float.MinValue   // Quiet cannot demote
    };
}
