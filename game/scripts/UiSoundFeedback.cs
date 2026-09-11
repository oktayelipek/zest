using Godot;

namespace Zest.Game;

/// <summary>Short synthesized UI tones; no external sound asset is needed for core feedback.</summary>
public partial class UiSoundFeedback : Node
{
    private AudioStreamPlayer _player = null!;
    public bool Enabled { get; set; } = true;

    public override void _Ready()
    {
        _player = new AudioStreamPlayer { Name = "UiTonePlayer", VolumeDb = -15f };
        AddChild(_player);
    }

    public void Play(bool positive = true)
    {
        if (!Enabled || _player is null) return;
        _player.Stream = CreateTone(positive ? 660f : 300f, positive ? .065f : .11f);
        _player.Play();
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
