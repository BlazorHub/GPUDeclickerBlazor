namespace AudioInputOutput_NAudio
{
    using System;
    using System.Linq;

    public class AudioData
    {
        private readonly float[] leftChannel;
        private readonly float[] rightChannel;
        private readonly int sampleRate;

        public AudioData(int sampleRate, float[] leftChannelSamples, float[] rightChannelSamples = null)
        {
            if (leftChannelSamples is null)
            {
                throw new ArgumentNullException(nameof(leftChannelSamples));
            }

            this.leftChannel = leftChannelSamples;
            this.rightChannel = rightChannelSamples;
            this.sampleRate = sampleRate;
        }

        public AudioData(int sampleRate, double[] leftChannel, double[] rightChannel = null)
        {
            if (leftChannel is null)
            {
                throw new ArgumentNullException(nameof(leftChannel));
            }

            this.leftChannel = leftChannel.Select(s => (float)s).ToArray();
            this.rightChannel = rightChannel?.Select(s => (float)s).ToArray();
            this.sampleRate = sampleRate;
        }

        public (int sampleRate, float[] leftChannel, float[] rightChannel) ToFloat() =>
            (this.sampleRate, this.leftChannel, this.rightChannel);

        public (int sampleRate, double[] leftChannel, double[] rightChannel) ToDouble() =>
            (this.sampleRate,
            this.leftChannel.Select(s => (double)s).ToArray(),
            this.rightChannel?.Select(s => (double)s).ToArray());
    }
}
