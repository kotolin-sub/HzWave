using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Audio.Effects;
using YukkuriMovieMaker.Plugin.Effects;

namespace HzWaveformEffect
{
    public enum WaveformType
    {
        [Display(Name = "sin波")]
        Sine,
        [Display(Name = "短径波")]
        Square,
        [Display(Name = "三角波")]
        Triangle,
        [Display(Name = "のこぎり波")]
        Sawtooth
    }

    [AudioEffect("音波(周波数)", ["エフェクト"], [])]
    public class WaveformGenerator : AudioEffectBase
    {
        public override string Label => "音波(周波数)";

        // 周波数設定
        [Display(Name = "周波数")]
        [AnimationSlider("F2", "Hz", 10, 20000)]
        public Animation Frequency { get; } = new Animation(440, 10, 20000);

        [Display(Name = "振幅")]
        [AnimationSlider("F2", "%", 0, 100)]
        public Animation Amplitude { get; } = new Animation(50, 0, 100);

        [Display(GroupName = "波形設定")]
        [EnumComboBox]
        public WaveformType Waveform { get => waveform; set => Set(ref waveform, value); }
        private WaveformType waveform = WaveformType.Sine;

        public override IAudioEffectProcessor CreateAudioEffect(TimeSpan duration)
            => new WaveformProc(this, duration);

        public override IEnumerable<string> CreateExoAudioFilters(int k, ExoOutputDescription e) => [];

        protected override IEnumerable<IAnimatable> GetAnimatables() => [Frequency, Amplitude];
    }

    internal class WaveformProc : AudioEffectProcessorBase
    {
        private readonly WaveformGenerator settings;
        private readonly TimeSpan duration;
        private long currentPosition;

        public override int Hz => Input?.Hz ?? 44100;
        public override long Duration => (long)(Hz * duration.TotalSeconds);

        public WaveformProc(WaveformGenerator generator, TimeSpan duration)
        {
            this.settings = generator;
            this.duration = duration;
        }

        protected override void seek(long position)
        {
            currentPosition = position;
        }

        protected override int read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i += 2)
            {
                long currentSample = (currentPosition + i) / 2;
                double totalSamples = Duration;

                double frequency = settings.Frequency.GetValue(currentSample, (long)totalSamples, Hz);
                double amplitude = settings.Amplitude.GetValue(currentSample, (long)totalSamples, Hz) / 100.0;

                double sampleTime = currentSample / (double)Hz;
                double waveformValue = GenerateWaveform(settings.Waveform, frequency, sampleTime);
                float sample = (float)(waveformValue * amplitude);

                buffer[offset + i] = sample;
                buffer[offset + i + 1] = sample;
            }

            currentPosition += count;
            return count;
        }

        private double GenerateWaveform(WaveformType type, double frequency, double time)
        {
            switch (type)
            {
                case WaveformType.Sine:
                    return Math.Sin(2 * Math.PI * frequency * time);
                case WaveformType.Square:
                    return Math.Sign(Math.Sin(2 * Math.PI * frequency * time));
                case WaveformType.Triangle:
                    return 2 * Math.Abs(2 * (time * frequency - Math.Floor(time * frequency + 0.5))) - 1;
                case WaveformType.Sawtooth:
                    return 2 * (time * frequency - Math.Floor(time * frequency)) - 1;
                default:
                    return 0;
            }
        }
    }
}