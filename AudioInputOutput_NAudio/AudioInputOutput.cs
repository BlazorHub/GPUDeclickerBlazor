using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NAudio.Wave;
using NLayer.NAudioSupport;

namespace AudioInputOutput_NAudio
{
    public class AudioInputOutput
    {
        public (bool success, MemoryStream stream) SaveToMemoryStream(AudioData data)
        {
            (var sampleRate, var leftChannel, var rightChannel) = data.ToFloat();
            var bufferSamples = GetBuffer(leftChannel, rightChannel);

            var memoryStream = new MemoryStream();
            using (var writer = new WaveFileWriter(
                memoryStream, 
                new WaveFormat(sampleRate, 
                rightChannel is null ? 1 : 2)))
                    writer.WriteSamples(bufferSamples, 0, bufferSamples.Length);

            return (true, memoryStream);
        }

        private float[] GetBuffer(float[] leftChannel, float[] rightChannel)
        {
            var channelsNumber = rightChannel is null ? 1 : 2;
            var bufferSamples = new float[leftChannel.Length * channelsNumber];

            for (var index = 0; index < leftChannel.Length; index++)
            {
                bufferSamples[index * channelsNumber] = leftChannel[index];
                if (channelsNumber == 2)
                    bufferSamples[index * channelsNumber + 1] = rightChannel[index];
            }

            return bufferSamples;
        }

        public async Task<(string errorMessage, AudioData data)> LoadAudioFromHttpAsync(Uri url)
        {
            if (url is null)
                return ("URL can not be null", null);

            using var stream = GetStreamFromUrl(url);
            return await LoadAudioFromStreamAsync(stream, url.ToString())
                .ConfigureAwait(false);
        }

        public async Task<(string errorMessage, AudioData data)> LoadAudioFromStreamAsync(Stream stream, string name)
        {
            if (stream is null || name is null)
                return ("Stream or Name can not be null", null);

            List<double> samplesLeft = new List<double>();
            List<double> samplesRight = new List<double>();
            var inputSampleRate = -1;
            var errorMessage = String.Empty;

            try
            {
                using var memoryStream = await GetContentFromStreamAsync(stream)
                    .ConfigureAwait(false);
                // Convert content to samples
                using var reader = GetReader(name, memoryStream);
                UpdateSamples(reader.ToSampleProvider(), samplesLeft, samplesRight);
                inputSampleRate = reader.WaveFormat.SampleRate;
            }
            catch (Exception e)
            {                
                errorMessage = e.Message;
            }

            if (samplesRight.Any()) // stereo
            {
                return (String.Empty, new AudioData(inputSampleRate, samplesLeft.ToArray(), samplesRight.ToArray()));
            }
            else if (samplesLeft.Any()) // mono
            {
                return (String.Empty, new AudioData(inputSampleRate, samplesLeft.ToArray()));
            }
            else // error
                return (errorMessage, null);
        }

        private static Stream GetStreamFromUrl(Uri url) =>
            WebRequest.Create(url)
                .GetResponse().GetResponseStream();

        private static async Task<MemoryStream> GetContentFromStreamAsync(Stream stream)
        {
            var memoryStream = new MemoryStream();

            var buffer = new byte[32768];
            int byteCount;
            while ((byteCount = await stream.ReadAsync(buffer, 0, buffer.Length)
                .ConfigureAwait(false)) > 0)
            {
                memoryStream.Write(buffer, 0, byteCount);
            }

            // Reset memory stream position
            memoryStream.Position = 0;

            return memoryStream;
        }

        private static WaveStream GetReader(string fileName, MemoryStream memoryStream)
        {
            if (fileName.EndsWith("wav", true, CultureInfo.InvariantCulture))
                return new WaveFileReader(memoryStream);
            else if (fileName.EndsWith("mp3", true, CultureInfo.InvariantCulture))
                return new Mp3FileReader(
                    memoryStream, 
                    new Mp3FileReader.FrameDecompressorBuilder(
                        wf => new Mp3FrameDecompressor(wf)));
            else
                throw new FormatException("This audio file is not supported");
        }

        private static void UpdateSamples(
            ISampleProvider sampleProvider,
            List<double> samplesLeft,
            List<double> samplesRight)
        {
            if (sampleProvider is null)
                throw new ArgumentNullException(nameof(sampleProvider));

            var bufferSamples = new float[16384];
            var channelsCount = sampleProvider.WaveFormat.Channels;

            int sampleCount;
            while ((sampleCount = sampleProvider.Read(bufferSamples, 0, bufferSamples.Length)) > 0)
            {
                for (var index = 0; index < sampleCount; index += channelsCount)
                {
                    samplesLeft.Add(bufferSamples[index]);
                    if (channelsCount == 2)
                    {
                        samplesRight.Add(bufferSamples[index + 1]);
                    }
                }
            }
        }
    }
} 