using System;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Citizens;
using OrbitalRings.Core;

namespace OrbitalRings.Audio;

/// <summary>
/// Wish fulfillment celebration: plays a warm G4 chime and spawns gold sparkle
/// particles at the citizen's 3D position on every WishFulfilled event.
///
/// Chime: G4 (392 Hz) with octave + perfect fifth harmonics, exponential decay
/// envelope for warm sustain. Distinct from placement chime (C5 523 Hz).
///
/// Particles: Gold/yellow GPUParticles3D burst at citizen position, upward spread
/// with light gravity for a floaty, celebratory feel. Self-cleaning via Finished event.
///
/// Extends SafeNode for proper event subscribe/unsubscribe lifecycle.
/// </summary>
public partial class WishCelebration : SafeNode
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Chime fundamental frequency: G4 (distinct from placement chime C5 at 523 Hz).</summary>
    private const float ChimeFrequency = 392f;

    /// <summary>Chime duration in seconds (longer than placement chime 0.15s for more presence).</summary>
    private const float ChimeDuration = 0.5f;

    /// <summary>Chime playback volume in dB.</summary>
    private const float ChimeVolumeDb = -4f;

    /// <summary>Sample rate for chime generation.</summary>
    private const int SampleRate = 22050;

    /// <summary>Particle count for gold sparkle burst.</summary>
    private const int ParticleAmount = 20;

    /// <summary>Particle lifetime in seconds.</summary>
    private const float ParticleLifetime = 0.8f;

    /// <summary>Particle explosiveness (0-1, higher = more simultaneous burst).</summary>
    private const float ParticleExplosiveness = 0.85f;

    /// <summary>Vertical offset above citizen center for particle spawn.</summary>
    private const float ParticleVerticalOffset = 0.3f;

    // -------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------

    private AudioStreamPlayer _chimePlayer;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        _chimePlayer = new AudioStreamPlayer
        {
            Stream = GenerateWishChime(),
            VolumeDb = ChimeVolumeDb
        };
        AddChild(_chimePlayer);
    }

    // -------------------------------------------------------------------------
    // Event subscriptions (SafeNode lifecycle)
    // -------------------------------------------------------------------------

    protected override void SubscribeEvents()
    {
        if (GameEvents.Instance == null) return;
        GameEvents.Instance.WishFulfilled += OnWishFulfilled;
    }

    protected override void UnsubscribeEvents()
    {
        if (GameEvents.Instance == null) return;
        GameEvents.Instance.WishFulfilled -= OnWishFulfilled;
    }

    // -------------------------------------------------------------------------
    // Wish fulfillment handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Handles WishFulfilled event: plays chime and spawns gold sparkles at citizen position.
    /// Chime always plays. Sparkles only spawn if citizen is found and not visiting.
    /// </summary>
    private void OnWishFulfilled(string citizenName, string wishType)
    {
        // 1. Play celebration chime (always, regardless of citizen visibility)
        _chimePlayer?.Play();

        // 2. Find citizen node by name to get 3D position for sparkles
        if (CitizenManager.Instance == null) return;

        CitizenNode targetCitizen = null;
        var citizens = CitizenManager.Instance.Citizens;
        for (int i = 0; i < citizens.Count; i++)
        {
            if (citizens[i].Data.CitizenName == citizenName)
            {
                targetCitizen = citizens[i];
                break;
            }
        }

        // 3. Spawn gold sparkles at citizen position (skip if not found or visiting)
        if (targetCitizen != null && IsInstanceValid(targetCitizen) && !targetCitizen.IsVisiting)
        {
            SpawnGoldSparkles(targetCitizen.GlobalPosition);
        }
    }

    // -------------------------------------------------------------------------
    // Gold sparkle particles
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawns a one-shot GPUParticles3D gold sparkle burst at the specified position.
    /// Reuses the established pattern from PlacementFeedback.OnDemolishConfirmed().
    /// Self-cleanup via Finished event (no orphan nodes).
    /// </summary>
    private void SpawnGoldSparkles(Vector3 citizenPosition)
    {
        var particles = new GpuParticles3D();
        particles.OneShot = true;
        particles.Emitting = false;
        particles.Amount = ParticleAmount;
        particles.Lifetime = ParticleLifetime;
        particles.Explosiveness = ParticleExplosiveness;

        var material = new ParticleProcessMaterial();
        material.Direction = new Vector3(0, 1, 0);
        material.Spread = 60f;
        material.InitialVelocityMin = 1.5f;
        material.InitialVelocityMax = 3.5f;
        material.Gravity = new Vector3(0, -2f, 0);    // Lighter gravity for floaty feel
        material.ScaleMin = 0.03f;
        material.ScaleMax = 0.08f;
        material.Color = new Color(1.0f, 0.85f, 0.3f, 0.9f); // Gold/yellow

        particles.ProcessMaterial = material;
        particles.DrawPass1 = new SphereMesh { Radius = 0.03f, Height = 0.06f };
        particles.Position = citizenPosition + new Vector3(0, ParticleVerticalOffset, 0);

        GetTree().Root.AddChild(particles);

        // Restart + Emitting workaround for Godot one-shot GPUParticles3D
        particles.Restart();
        particles.Emitting = true;

        // Self-cleanup after particles finish (established pattern)
        particles.Finished += () =>
        {
            if (IsInstanceValid(particles))
                particles.QueueFree();
        };
    }

    // -------------------------------------------------------------------------
    // Procedural chime generation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates a warm G4 chime with harmonics and exponential decay envelope.
    ///
    /// Harmonic structure:
    ///   - G4 fundamental 392 Hz (amplitude 0.5)
    ///   - G5 octave 784 Hz (amplitude 0.2)
    ///   - D5 perfect fifth 587.33 Hz (amplitude 0.15)
    ///
    /// Exponential decay exp(-3t) gives warmer sustain than the placement chime's
    /// linear decay, making the reward sound feel more resonant and satisfying.
    /// </summary>
    private static AudioStreamWav GenerateWishChime()
    {
        int sampleCount = (int)(SampleRate * ChimeDuration);
        byte[] data = new byte[sampleCount * 2]; // 16-bit mono = 2 bytes per sample

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            float envelope = MathF.Exp(-3f * t); // Exponential decay (warmer than linear)

            float sample = MathF.Sin(2 * MathF.PI * 392f * t) * 0.5f;       // G4 fundamental
            sample += MathF.Sin(2 * MathF.PI * 784f * t) * 0.2f;            // G5 octave
            sample += MathF.Sin(2 * MathF.PI * 587.33f * t) * 0.15f;        // D5 perfect fifth
            sample *= envelope * 0.5f;

            short pcm = (short)(sample * short.MaxValue);
            data[i * 2] = (byte)(pcm & 0xFF);
            data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        var stream = new AudioStreamWav();
        stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        stream.MixRate = SampleRate;
        stream.Data = data;
        return stream;
    }
}
