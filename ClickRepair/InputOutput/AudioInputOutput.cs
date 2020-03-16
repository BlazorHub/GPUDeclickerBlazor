using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GPUDeclickerUWP.Model.Data;
using NAudio.Wave;
using NLayer.NAudioSupport;

namespace GPUDeclickerUWP.Model.InputOutput
{
    public class AudioInputOutput
    {
        private AudioData _audioData;

        public AudioData GetAudioData()
        {
            return _audioData;
        }

        public void SetAudioData(AudioData value)
        {
            _audioData = value;
        }

        public async Task<(bool success, string error)> LoadAudioFromHttpAsync(Uri url)
        {
            return await Task.Run(() => LoadAudioFromHttp(url)).ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We will show load errors to users")]
        private (bool success, string error) LoadAudioFromHttp(Uri url)
        {
            List<float> samplesLeft = new List<float>();
            List<float> samplesRight = new List<float>();
            var error = String.Empty;
            bool success;

            try
            {
                // Get content
                using (var memoryStream = GetStreamFromUrl(url))
                    // Convert content to samples
                    using (var reader = GetReader(url, memoryStream))
                        GetSamples(reader.ToSampleProvider(), ref samplesLeft, ref samplesRight);
                
                error = "OK";
            }
            catch (Exception e)
            {                
                error = e.Message;
            }
            finally
            {
                if (samplesRight.Any())
                {
                    SetAudioData(new AudioDataStereo(samplesLeft.ToArray(), samplesRight.ToArray()));
                    success = true;
                }
                else if (samplesLeft.Any())
                {
                    SetAudioData(new AudioDataMono(samplesLeft.ToArray()));
                    success = true;
                }
                else
                    success = false;
            }

            return (success, error);
        }

        private static MemoryStream GetStreamFromUrl(Uri url)
        {
            var memoryStream = new MemoryStream();

            // Get all content to memory stream
            using (var stream = WebRequest.Create(url)
                .GetResponse().GetResponseStream())
            {
                var bufferDownload = new byte[32768];
                int byteCount;
                while ((byteCount = stream.Read(bufferDownload, 0, bufferDownload.Length)) > 0)
                    memoryStream.Write(bufferDownload, 0, byteCount);
            }

            // Reset memory stream position
            memoryStream.Position = 0;

            return memoryStream;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private static WaveStream GetReader(Uri url, MemoryStream memoryStream)
        {
            if (url.ToString().EndsWith("wav", true, CultureInfo.InvariantCulture))
                return new WaveFileReader(memoryStream);
            else if (url.ToString().EndsWith("mp3", true, CultureInfo.InvariantCulture))
                return new Mp3FileReader(
                    memoryStream, 
                    new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf)));
            else
                throw new FormatException("This audio file not supported");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private static void GetSamples(ISampleProvider sampleProvider, ref List<float> samplesLeft, ref List<float> samplesRight)
        {
            if (sampleProvider is null)
                throw new FormatException("Sample provider was not set");

            var bufferSamples = new float[16384];
            int sampleCount;
            while ((sampleCount = sampleProvider.Read(bufferSamples, 0, bufferSamples.Length)) > 0)
                if (sampleProvider.WaveFormat.Channels == 1)
                    // Mono
                    samplesLeft.AddRange(bufferSamples.Take(sampleCount));
                else if (sampleProvider.WaveFormat.Channels == 2)
                    // Stereo
                    for (var index = 0; index < sampleCount; index += 2)
                    {
                        samplesLeft.Add(bufferSamples[index]);
                        samplesRight.Add(bufferSamples[index + 1]);
                    }
                else
                    throw new FormatException("More than two channels audio not supported");
        }
    }
} 