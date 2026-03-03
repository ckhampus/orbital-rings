using System;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Core;
using OrbitalRings.Ring;

namespace OrbitalRings.Build;

/// <summary>
/// Audio-visual feedback system for room placement, demolition, and invalid actions.
///
/// Placement: squash-and-stretch bounce + white emission flash + procedural chime (523 Hz C5).
/// Demolish: GPUParticles3D one-shot puff (soap bubble pop) + procedural pop (220 Hz A3).
/// Invalid: red flash on rejected segment (0.2s restore) + error buzz (110 Hz A2).
///
/// All audio is procedurally generated via AudioStreamWav -- zero external assets needed.
/// Instantiated as a child of BuildManager (Autoload) for guaranteed scene tree presence.
///
/// Extends SafeNode for proper event subscribe/unsubscribe lifecycle.
/// </summary>
public partial class PlacementFeedback : SafeNode
{
    // -------------------------------------------------------------------------
    // Audio players
    // -------------------------------------------------------------------------

    private AudioStreamPlayer _placementPlayer;
    private AudioStreamPlayer _demolishPlayer;
    private AudioStreamPlayer _errorPlayer;

    // -------------------------------------------------------------------------
    // Tween tracking (kill-before-create to prevent stacking)
    // -------------------------------------------------------------------------

    private Tween _placementTween;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        // Generate procedural audio streams and create players
        _placementPlayer = new AudioStreamPlayer
        {
            Stream = GenerateTone(523.25f, 0.15f),
            VolumeDb = -6f
        };
        _demolishPlayer = new AudioStreamPlayer
        {
            Stream = GenerateTone(220f, 0.05f),
            VolumeDb = -4f
        };
        _errorPlayer = new AudioStreamPlayer
        {
            Stream = GenerateTone(110f, 0.1f),
            VolumeDb = -8f
        };

        AddChild(_placementPlayer);
        AddChild(_demolishPlayer);
        AddChild(_errorPlayer);
    }

    // -------------------------------------------------------------------------
    // Event subscriptions (SafeNode lifecycle)
    // -------------------------------------------------------------------------

    protected override void SubscribeEvents()
    {
        if (GameEvents.Instance == null) return;
        GameEvents.Instance.RoomPlacementConfirmed += OnPlacementConfirmed;
        GameEvents.Instance.RoomDemolishConfirmed += OnDemolishConfirmed;
        GameEvents.Instance.PlacementInvalid += OnPlacementInvalid;
    }

    protected override void UnsubscribeEvents()
    {
        if (GameEvents.Instance == null) return;
        GameEvents.Instance.RoomPlacementConfirmed -= OnPlacementConfirmed;
        GameEvents.Instance.RoomDemolishConfirmed -= OnDemolishConfirmed;
        GameEvents.Instance.PlacementInvalid -= OnPlacementInvalid;
    }

    // -------------------------------------------------------------------------
    // Placement feedback: squash-and-stretch + white flash + chime
    // -------------------------------------------------------------------------

    private void OnPlacementConfirmed(MeshInstance3D roomMesh, Vector3 position, Color categoryColor)
    {
        if (roomMesh == null || !IsInstanceValid(roomMesh)) return;

        // Kill any existing placement tween to prevent stacking on rapid placement
        _placementTween?.Kill();

        // --- Squash-and-stretch animation ---
        Vector3 finalScale = roomMesh.Scale;
        // Start squashed wide and flat, overshoot tall and narrow, then settle
        Vector3 squash = new(finalScale.X * 1.15f, finalScale.Y * 0.6f, finalScale.Z * 1.15f);
        Vector3 overshoot = new(finalScale.X * 0.92f, finalScale.Y * 1.35f, finalScale.Z * 0.92f);

        roomMesh.Scale = squash;

        _placementTween = roomMesh.CreateTween();
        _placementTween.TweenProperty(roomMesh, "scale", overshoot, 0.1f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        _placementTween.TweenProperty(roomMesh, "scale", finalScale, 0.15f)
            .SetTrans(Tween.TransitionType.Elastic)
            .SetEase(Tween.EaseType.Out);

        // --- White emission flash ---
        var mat = roomMesh.MaterialOverride as StandardMaterial3D;
        if (mat != null)
        {
            mat.EmissionEnabled = true;
            mat.Emission = Colors.White;
            mat.EmissionEnergyMultiplier = 2.0f;

            var flashTween = roomMesh.CreateTween();
            flashTween.TweenProperty(mat, "emission_energy_multiplier", 0.0f, 0.3f)
                .SetEase(Tween.EaseType.Out);
            flashTween.TweenCallback(Callable.From(() =>
            {
                if (IsInstanceValid(roomMesh) && mat != null)
                    mat.EmissionEnabled = false;
            }));
        }

        // --- Placement chime ---
        _placementPlayer?.Play();
    }

    // -------------------------------------------------------------------------
    // Demolish feedback: particle puff (soap bubble pop) + pop sound
    // -------------------------------------------------------------------------

    private void OnDemolishConfirmed(Vector3 position, Color roomColor)
    {
        // --- Particle puff (soap bubble pop) ---
        var particles = new GpuParticles3D();
        particles.OneShot = true;
        particles.Emitting = false;
        particles.Amount = 12;
        particles.Lifetime = 0.6f;
        particles.Explosiveness = 0.9f;

        var material = new ParticleProcessMaterial();
        material.Direction = new Vector3(0, 1, 0);
        material.Spread = 45f;
        material.InitialVelocityMin = 1.0f;
        material.InitialVelocityMax = 3.0f;
        material.Gravity = new Vector3(0, -4f, 0);
        material.ScaleMin = 0.05f;
        material.ScaleMax = 0.12f;
        material.Color = new Color(roomColor.R, roomColor.G, roomColor.B, 0.8f);

        particles.ProcessMaterial = material;
        particles.DrawPass1 = new SphereMesh { Radius = 0.04f, Height = 0.08f };

        particles.Position = position;
        GetTree().Root.AddChild(particles);

        // Restart + Emitting workaround for Godot one-shot GPUParticles3D bug
        particles.Restart();
        particles.Emitting = true;

        // Self-cleanup after particles finish (no orphan nodes)
        particles.Finished += () =>
        {
            if (IsInstanceValid(particles))
                particles.QueueFree();
        };

        // --- Demolish pop sound ---
        _demolishPlayer?.Play();
    }

    // -------------------------------------------------------------------------
    // Invalid placement feedback: red flash on segment + error buzz
    // -------------------------------------------------------------------------

    private void OnPlacementInvalid(int flatIndex)
    {
        // Find the ring visual to flash the segment
        var ringVisual = GetTree().Root.FindChild("Ring", true, false) as RingVisual;
        if (ringVisual != null)
        {
            var segMesh = ringVisual.GetSegmentMesh(flatIndex);
            if (segMesh != null)
            {
                // Store original material, apply red flash, then restore
                var originalMat = segMesh.MaterialOverride;
                var flashMat = new StandardMaterial3D
                {
                    AlbedoColor = RoomColors.InvalidFlash,
                    EmissionEnabled = true,
                    Emission = new Color(0.95f, 0.2f, 0.2f),
                    EmissionEnergyMultiplier = 1.5f
                };
                segMesh.MaterialOverride = flashMat;

                // Restore original material after 0.2 seconds
                var restoreTween = CreateTween();
                restoreTween.TweenCallback(Callable.From(() =>
                {
                    if (IsInstanceValid(segMesh))
                        segMesh.MaterialOverride = originalMat;
                })).SetDelay(0.2f);
            }
        }

        // --- Error buzz ---
        _errorPlayer?.Play();
    }

    // -------------------------------------------------------------------------
    // Procedural audio generation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates a simple sine wave tone with linear decay envelope.
    /// Returns an AudioStreamWav (16-bit mono PCM) ready for playback.
    /// Used for all feedback sounds -- zero external audio assets needed.
    /// </summary>
    /// <param name="frequency">Tone frequency in Hz (e.g., 523.25 for C5).</param>
    /// <param name="duration">Duration in seconds.</param>
    /// <param name="sampleRate">Sample rate (default 22050 Hz -- sufficient for UI tones).</param>
    private static AudioStreamWav GenerateTone(float frequency, float duration, int sampleRate = 22050)
    {
        int sampleCount = (int)(sampleRate * duration);
        byte[] data = new byte[sampleCount * 2]; // 16-bit mono = 2 bytes per sample

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1.0f - (t / duration); // Linear decay
            float sample = MathF.Sin(2 * MathF.PI * frequency * t) * envelope * 0.5f;
            short pcm = (short)(sample * short.MaxValue);
            data[i * 2] = (byte)(pcm & 0xFF);
            data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        var stream = new AudioStreamWav();
        stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        stream.MixRate = sampleRate;
        stream.Data = data;
        return stream;
    }
}
