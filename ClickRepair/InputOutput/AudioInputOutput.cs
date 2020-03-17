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

        public (bool success, MemoryStream stream) SaveAudioToStream()
        {
            if (_audioData is null)
                return (false, null);

            var bufferSamples = GetAllSamples();
                
            var memoryStream = new MemoryStream();
            using (var writer = new WaveFileWriter(memoryStream, new WaveFormat(44100, _audioData.IsStereo ? 2 : 1)))
                writer.WriteSamples(bufferSamples, 0, bufferSamples.Length);

            return (true, memoryStream);
        }

        private float[] GetAllSamples()
        {
            var channelsNumber = _audioData.IsStereo ? 2 : 1;
            var bufferSamples = new float[_audioData.LengthSamples() * channelsNumber];

            for (var index = 0; index < _audioData.LengthSamples(); index++)
            {
                _audioData.SetCurrentChannelType(ChannelType.Left);
                bufferSamples[index * channelsNumber] = _audioData.GetOutputSample(index);
                if (_audioData.IsStereo)
                {
                    _audioData.SetCurrentChannelType(ChannelType.Right);
                    bufferSamples[index * channelsNumber + 1] = _audioData.GetOutputSample(index);
                }
            }

            return bufferSamples;
        }

        public void SetAudioData(AudioData value)
        {
            _audioData = value;
        }

        public async Task<(bool success, string error)> LoadAudioFromHttpAsync(Uri url)
        {
            if (url is null)
                return (false, "URL can not be null");

            using var stream = GetStreamFromUrl(url);
            return await LoadAudioFromStreamAsync(stream, url.ToString()).ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "We will show load errors to users")]
        public async Task<(bool success, string error)> LoadAudioFromStreamAsync(Stream stream, string name)
        {
            if (stream is null || name is null)
                return (false, "Stream or Name can not be null");

            List<float> samplesLeft = new List<float>();
            List<float> samplesRight = new List<float>();
            var error = String.Empty;
            bool success;

            try
            {
                using (var memoryStream = await GetContentFromStreamAsync(stream)
                    .ConfigureAwait(false))
                    // Convert content to samples
                    using (var reader = GetReader(name, memoryStream))
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

        private static Stream GetStreamFromUrl(Uri url)
        {
            return WebRequest.Create(url)
                .GetResponse().GetResponseStream();
        }

        private static async Task<MemoryStream> GetContentFromStreamAsync(Stream stream)
        {
            var memoryStream = new MemoryStream();

            var buffer = new byte[32768];
            int byteCount;
            while ((byteCount = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                memoryStream.Write(buffer, 0, byteCount);

            // Reset memory stream position
            memoryStream.Position = 0;

            return memoryStream;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
        private static WaveStream GetReader(string fileName, MemoryStream memoryStream)
        {
            if (fileName.EndsWith("wav", true, CultureInfo.InvariantCulture))
                return new WaveFileReader(memoryStream);
            else if (fileName.EndsWith("mp3", true, CultureInfo.InvariantCulture))
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