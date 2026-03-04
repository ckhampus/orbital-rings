using System;
using Godot;

namespace OrbitalRings.Audio;

/// <summary>
/// Procedural ambient space drone that plays continuously during gameplay.
///
/// Generates a low-frequency hum with harmonics (fundamental + perfect fifth + octave)
/// and gentle amplitude modulation, creating the feeling of distant space station
/// life support systems. Buffer is calculated as an exact multiple of the base
/// frequency period to ensure seamless looping with no audible click at the seam.
///
/// All audio is procedurally generated -- zero external assets needed.
/// Added as a child of the main scene or an Autoload for persistent playback.
/// </summary>
public partial class AmbientDrone : Node
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Base frequency of the drone hum (Hz).</summary>
    private const float BaseFrequency = 60f;

    /// <summary>Buffer duration target in seconds (actual length adjusted for seamless loop).</summary>
    private const float BufferDuration = 4.0f;

    /// <summary>Sample rate -- sufficient for low-frequency drone content.</summary>
    private const int SampleRate = 22050;

    /// <summary>Playback volume in dB (quiet background, not foreground).</summary>
    private const float VolumeDb = -12f;

    // -------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------

    private AudioStreamPlayer _player;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        var stream = GenerateAmbientDrone();

        _player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = VolumeDb
        };
        AddChild(_player);

        _player.Play();
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Starts drone playback if not already playing.</summary>
    public void StartPlaying()
    {
        if (_player != null && !_player.Playing)
            _player.Play();
    }

    /// <summary>Stops drone playback.</summary>
    public void StopPlaying()
    {
        _player?.Stop();
    }

    // -------------------------------------------------------------------------
    // Procedural audio generation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates a low-frequency ambient drone with harmonics and gentle modulation.
    ///
    /// Harmonic structure:
    ///   - Fundamental at 60 Hz (amplitude 0.4) -- low machinery hum
    ///   - Perfect fifth at 90 Hz (amplitude 0.15) -- subtle harmonic richness
    ///   - Octave at 120 Hz (amplitude 0.1) -- upper warmth
    ///
    /// Amplitude modulation: slow 0.3 Hz wobble (0.85 + 0.15 * sin) for organic feel.
    /// Overall volume scaled by 0.3 to keep the drone as quiet background texture.
    ///
    /// Buffer length is an exact multiple of the base frequency period to ensure
    /// the waveform starts and ends at the same phase, eliminating clicks at the loop point.
    /// </summary>
    private static AudioStreamWav GenerateAmbientDrone()
    {
        // Calculate buffer length as exact multiple of base frequency period
        // Period in samples = sampleRate / baseFreq
        float period = SampleRate / BaseFrequency;
        int periods = (int)(BufferDuration * BaseFrequency);
        int sampleCount = (int)(periods * period);

        byte[] data = new byte[sampleCount * 2]; // 16-bit mono = 2 bytes per sample

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;

            // Layered harmonics
            float sample = MathF.Sin(2 * MathF.PI * BaseFrequency * t) * 0.4f;          // Fundamental
            sample += MathF.Sin(2 * MathF.PI * BaseFrequency * 1.5f * t) * 0.15f;       // Perfect fifth
            sample += MathF.Sin(2 * MathF.PI * BaseFrequency * 2f * t) * 0.1f;          // Octave

            // Gentle amplitude modulation (slow 0.3 Hz wobble)
            float mod = 0.85f + 0.15f * MathF.Sin(2 * MathF.PI * 0.3f * t);
            sample *= mod * 0.3f; // Keep quiet

            short pcm = (short)(sample * short.MaxValue);
            data[i * 2] = (byte)(pcm & 0xFF);
            data[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        var stream = new AudioStreamWav();
        stream.Format = AudioStreamWav.FormatEnum.Format16Bits;
        stream.MixRate = SampleRate;
        stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
        stream.LoopBegin = 0;
        stream.LoopEnd = sampleCount;
        stream.Data = data;
        return stream;
    }
}
