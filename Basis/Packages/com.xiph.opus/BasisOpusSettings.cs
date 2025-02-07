using UnityEngine;
using UnityOpus;
public static class BasisOpusSettings
{
    public static int RecordingFullLength = 1;
    public static int BitrateKPS = 64000; // 128 kbps
    /// <summary>
    /// where 0 is the fastest on the cpu
    /// and 10 is the most performance hoggy
    /// recommend 10 as network performance is better.
    /// </summary>
    public static int Complexity = 10;
    public static SamplingFrequency SamplingFrequency = SamplingFrequency.Frequency_48000;
    public static NumChannels NumChannels = NumChannels.Mono;
    public static OpusApplication OpusApplication = OpusApplication.Audio;
    public static OpusSignal OpusSignal = OpusSignal.Auto;
    public static float DesiredDurationInSeconds = 0.02f;
    public static int GetSampleFreq()
    {
        return SampleFreqToInt(SamplingFrequency);
    }
    public static int CalculateDesiredTime()
    {
        return Mathf.CeilToInt(DesiredDurationInSeconds * GetSampleFreq());
    }
    public static float[] CalculateProcessBuffer()
    {
        return new float[CalculateDesiredTime()];
    }
    public static int GetChannelAsInt()
    {
        return GetChannelAsInt(NumChannels);
    }
    public static int GetChannelAsInt(NumChannels SamplingFrequency)
    {
        switch (SamplingFrequency)
        {
            case NumChannels.Mono:
                return 1;
            case NumChannels.Stereo:
                return 2;
            default:
                return 1;
        }
    }
    public static int SampleFreqToInt(SamplingFrequency SamplingFrequency)
    {
        switch (SamplingFrequency)
        {
            case SamplingFrequency.Frequency_48000:
                return 48000;
            case SamplingFrequency.Frequency_12000:
                return 12000;
            case SamplingFrequency.Frequency_8000:
                return 8000;
            case SamplingFrequency.Frequency_16000:
                return 16000;
            case SamplingFrequency.Frequency_24000:
                return 24000;
            default:
                return 48000;
        }
    }
}
