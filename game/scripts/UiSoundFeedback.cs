using Godot;

namespace Zest.Game;

/// <summary>Short synthesized UI tones; no external sound asset is needed for core feedback.</summary>
public partial class UiSoundFeedback : Node
{
    private AudioStreamPlayer _player = null!;
    public bool Enabled { get; set; } = true;
    private float _volumeDb = -12f;

    public override void _Ready()
    {
        _player = new AudioStreamPlayer { Name = "UiTonePlayer", VolumeDb = _volumeDb };
        AddChild(_player);
    }

    public void SetLevel(int level)
    {
        Enabled = level > 0;
        _volumeDb = level switch { >= 2 => -12f, 1 => -24f, _ => -60f };
        if (_player is not null) _player.VolumeDb = _volumeDb;
    }

    public void Play(bool positive = true)
    {
        if (!Enabled || _player is null) return;
        _player.Stream = CreateTone(positive ? 660f : 300f, positive ? .065f : .11f);
        _player.Play();
    }

    /// <summary>Bright two-note coin drop for a completed sale.</summary>
    public void PlayCoin()
    {
        if (!Enabled || _player is null) return;
        _player.Stream = CreateChord([880f, 1320f], .12f, gap: .035f);
        _player.Play();
    }

    /// <summary>Muted descending pair for a lost guest.</summary>
    public void PlayWalkaway()
    {
        if (!Enabled || _player is null) return;
        _player.Stream = CreateChord([440f, 330f], .16f, gap: .04f, amplitude: 3200);
        _player.Play();
    }

    private static AudioStreamWav CreateChord(float[] frequencies, float seconds, float gap = 0f, short amplitude = 4600)
    {
        const int sampleRate = 22050;
        int perTone = (int)(sampleRate * seconds);
        int gapSamples = (int)(sampleRate * gap);
        int total = perTone * frequencies.Length + gapSamples * Math.Max(0, frequencies.Length - 1);
        byte[] data = new byte[total * 2];
        int cursor = 0;
        for (int f = 0; f < frequencies.Length; f++)
        {
            for (int index = 0; index < perTone; index++)
            {
                float envelope = 1f - index / (float)perTone;
                short sample = (short)(Math.Sin(Math.Tau * frequencies[f] * index / sampleRate) * amplitude * envelope);
                data[cursor * 2] = (byte)(sample & 0xff);
                data[cursor * 2 + 1] = (byte)((sample >> 8) & 0xff);
                cursor++;
            }
            cursor += gapSamples;
        }
        return new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false,
        };
    }

    private static AudioStreamWav CreateTone(float frequency, float seconds)
    {
        const int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * seconds);
        byte[] data = new byte[sampleCount * 2];
        for (int index = 0; index < sampleCount; index++)
        {
            float envelope = 1f - index / (float)sampleCount;
            short sample = (short)(Math.Sin(Math.Tau * frequency * index / sampleRate) * 4600 * envelope);
            data[index * 2] = (byte)(sample & 0xff);
            data[index * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false,
        };
    }
}
