using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
/*using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage; */
using GPUDeclickerUWP.Model.Data;
using NAudio.Wave;

namespace GPUDeclickerUWP.Model.InputOutput
{
    /*
    /// <summary>
    ///     Declare COM interface
    /// </summary>
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    } */

    /// <summary>
    ///     This class uses AudioGraph API to transfer audio from file to
    ///     float array or back
    /// </summary>
    public class AudioInputOutput
    {
        private AudioData _audioData;
        //private int _audioDataCurrentPosition;
        //private AudioGraph _audioGraph;
        //private AudioFileInputNode _fileInputNode;
        //private AudioFileOutputNode _fileOutputNode;
        //private bool _finished;
        //private AudioFrameInputNode _frameInputNode;
        //private AudioFrameOutputNode _frameOutputNode;
        //private IProgress<double> _ioProgress;

        public AudioData GetAudioData()
        {
            return _audioData;
        }

        public void SetAudioData(AudioData value)
        {
            _audioData = value;
        }

        /*
        /// <summary>
        ///     Creates an instance of AudioGraph and sets io_progress
        /// </summary>
        public async Task<CreateAudioGraphResult> Init(
            Progress<double> progress)
        {
            // set io_progress var to show progress of input-output
            _ioProgress = progress;

            // initialize settings for AudioGraph
            var settings =
                new AudioGraphSettings(
                    AudioRenderCategory.Media
                );

            // if audioGraph was previously created
            if (_audioGraph != null)
            {
                _audioGraph.Dispose();
                _audioGraph = null;
            }

            var result =
                await AudioGraph.CreateAsync(settings);

            if (result.Status == AudioGraphCreationStatus.Success)
                _audioGraph = result.Graph;

            return result;
        } */

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
                using (var memoryStream = new MemoryStream())
                {
                    // Get all content to memory stream
                    using (var stream = WebRequest.Create(url)
                        .GetResponse().GetResponseStream())
                    {
                        var buffer = new byte[32768];
                        int byteCount;
                        while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                            memoryStream.Write(buffer, 0, byteCount);
                    }

                    // Reset memory stream position
                    memoryStream.Position = 0;

                    // Convert content to samples
                    if (url.ToString().EndsWith("wav", true, CultureInfo.InvariantCulture))
                    using (var waveReader = new WaveFileReader(memoryStream))
                    {
                        var sampleProvider = waveReader.ToSampleProvider();

                        var buffer = new float[16384];
                        int sampleCount;
                        while ((sampleCount = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                            if (sampleProvider.WaveFormat.Channels == 1)
                                // Mono
                                samplesLeft.AddRange(buffer.Take(sampleCount));
                            else if (sampleProvider.WaveFormat.Channels == 2)
                                // Stereo
                                for (var index = 0; index < sampleCount; index += 2)
                                {
                                    samplesLeft.Add(buffer[index]);
                                    samplesRight.Add(buffer[index + 1]);
                                }
                            else 
                                throw new FormatException("More than two channels audio not supported");
                    } 
                    else if (url.ToString().EndsWith("mp3", true, CultureInfo.InvariantCulture))
                        using (var waveReader = new Mp3FileReader(memoryStream))
                        {
                            var sampleProvider = waveReader.ToSampleProvider();

                            var buffer = new float[16384];
                            int sampleCount;
                            while ((sampleCount = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                                if (sampleProvider.WaveFormat.Channels == 1)
                                    // Mono
                                    samplesLeft.AddRange(buffer.Take(sampleCount));
                                else if (sampleProvider.WaveFormat.Channels == 2)
                                    // Stereo
                                    for (var index = 0; index < sampleCount; index += 2)
                                    {
                                        samplesLeft.Add(buffer[index]);
                                        samplesRight.Add(buffer[index + 1]);
                                    }
                                else 
                                    throw new FormatException("More than two channels audio not supported");
                        }
                    else
                        throw new FormatException("This audio file not supported");
                }
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

            /*
        /// <summary>
        ///     Creates instances of FileInputNode, FrameOutputNode, AudioData
        ///     starts AudioGraph, waits till loading of samples is finished
        /// </summary>
        /// <param name="file"> Input audio file</param>
        /// <param name="status"></param>
        public async Task<CreateAudioFileInputNodeResult>
            LoadAudioFromFile(
                StorageFile file,
                IProgress<string> status)
        {
            _finished = false;
            status.Report("Reading audio file");

            // Initialize FileInputNode
            var inputNodeCreationResult =
                await _audioGraph.CreateFileInputNodeAsync(file);

            if (inputNodeCreationResult.Status != AudioFileNodeCreationStatus.Success)
                return inputNodeCreationResult;

            _fileInputNode = inputNodeCreationResult.FileInputNode;


            // Read audio file encoding properties to pass them 
            //to FrameOutputNode creator

            var audioEncodingProperties =
                _fileInputNode.EncodingProperties;

            // Initialize FrameOutputNode and connect it to fileInputNode
            _frameOutputNode = _audioGraph.CreateFrameOutputNode(
                audioEncodingProperties
            );
            _frameOutputNode.Stop();
            _fileInputNode.AddOutgoingConnection(_frameOutputNode);

            // Add a handler for achiving the end of a file
            _fileInputNode.FileCompleted += FileInput_FileCompleted;
            // Add a handler which will transfer every audio frame into audioData 
            _audioGraph.QuantumStarted += FileInput_QuantumStarted;

            // Initialize audioData
            var numOfSamples = (int) Math.Ceiling(
                (decimal) 0.0000001
                * _fileInputNode.Duration.Ticks
                * _fileInputNode.EncodingProperties.SampleRate
            );
            if (audioEncodingProperties.ChannelCount == 1)
                SetAudioData(new AudioDataMono(new float[numOfSamples]));
            else
                SetAudioData(new AudioDataStereo(new float[numOfSamples],
                    new float[numOfSamples]));

            _audioDataCurrentPosition = 0;

            // Start process which will read audio file frame by frame
            // and will generated events QuantumStarted when a frame is in memory
            _audioGraph.Start();

            // didn't find a better way to wait for data
            while (!_finished)
                await Task.Delay(50);

            // crear status line
            status.Report("");

            return inputNodeCreationResult;
        } */

            /*
        /// <summary>
        ///     Starts when reading of samples from input audio file finished
        /// </summary>
        private void FileInput_FileCompleted(AudioFileInputNode sender, object args)
        {
            _audioGraph.Stop();
            _frameOutputNode?.Stop();
            _audioGraph.Dispose();
            _audioGraph = null;
            _finished = true;
            _ioProgress?.Report(0);
        }
        */
        /*
        /// <summary>
        ///     Starts every time when audio frame is read from a file
        /// </summary>
        private void FileInput_QuantumStarted(AudioGraph sender, object args)
        {
            // to not report too many times
            if (sender.CompletedQuantumCount % 100 == 0)
            {
                var numOfSamples =
                    0.0000001
                    * _fileInputNode.Duration.Ticks
                    * _fileInputNode.EncodingProperties.SampleRate;
                var dProgress =
                    100 *
                    (int) sender.CompletedQuantumCount
                    * sender.SamplesPerQuantum /
                    numOfSamples;
                _ioProgress?.Report(dProgress);
            }

            if (_audioDataCurrentPosition == 0) _frameOutputNode.Start();

            var frame = _frameOutputNode.GetFrame();
            ProcessInputFrame(frame);

            if (_finished)
            {
                _frameOutputNode?.Stop();
                _audioGraph?.Stop();
            }
        }

        /// <summary>
        ///     Transfers samples from a frame to AudioData
        /// </summary>
        private unsafe void ProcessInputFrame(AudioFrame frame)
        {
            using (var buffer =
                frame.LockBuffer(AudioBufferAccessMode.Read))
            using (var reference =
                buffer.CreateReference())
            {
                // Get data from current buffer
                (reference as IMemoryBufferByteAccess).GetBuffer(
                    out var dataInBytes,
                    out var capacityInBytes
                );
                // Discard first frame; it's full of zeros because of latency
                if (_audioGraph.CompletedQuantumCount == 1) return;

                var dataInFloat = (float*) dataInBytes;
                var capacityInFloat = capacityInBytes / sizeof(float);
                // Number of channels defines step between samples in buffer
                var channelCount = _fileInputNode.EncodingProperties.ChannelCount;
                // Transfer audio samples from buffer into audioData
                for (uint index = 0; index < capacityInFloat; index += channelCount)
                    if (_audioDataCurrentPosition < GetAudioData().LengthSamples())
                    {
                        GetAudioData().SetCurrentChannelType(ChannelType.Left);
                        GetAudioData().SetInputSample(
                            _audioDataCurrentPosition,
                            dataInFloat[index]
                        );
                        // if it's stereo
                        if (channelCount == 2)
                        {
                            GetAudioData().SetCurrentChannelType(ChannelType.Right);
                            GetAudioData().SetInputSample(
                                _audioDataCurrentPosition,
                                dataInFloat[index + 1]
                            );
                        }

                        _audioDataCurrentPosition++;
                    }
            }
        }

        public async Task<CreateAudioFileOutputNodeResult>
            SaveAudioToFile(
                StorageFile file,
                IProgress<string> status)
        {
            _finished = false;
            status.Report("Saving audio to file");

            var mediaEncodingProfile =
                CreateMediaEncodingProfile(file);

            if (!_audioData.IsStereo && mediaEncodingProfile.Audio != null)
                    mediaEncodingProfile.Audio.ChannelCount = 1;

            // Initialize FileOutputNode
            var result =
                await _audioGraph.CreateFileOutputNodeAsync(file, mediaEncodingProfile);

            if (result.Status != AudioFileNodeCreationStatus.Success)
                return result;

            _fileOutputNode = result.FileOutputNode;
            _fileOutputNode.Stop();

            // Initialize FrameInputNode and connect it to fileOutputNode
            _frameInputNode = _audioGraph.CreateFrameInputNode(
                // EncodingProprties are different than for input file
                _fileOutputNode.EncodingProperties
                //audioEncodingProperties
            );

            _frameInputNode.AddOutgoingConnection(_fileOutputNode);
            _frameInputNode.Stop();

            // Add a handler which will transfer every audioData sample to audio frame
            _frameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;

            _audioDataCurrentPosition = 0;

            // Start process which will write audio file frame by frame
            // and will generated events QuantumStarted 
            _audioGraph.Start();
            // don't start fileOutputNode yet because it will record zeros

            // because we initialised frameInputNode in Stop mode we need to start it
            _frameInputNode.Start();

            // didn't find a better way to wait for writing to file
            while (!_finished)
                await Task.Delay(50);

            // when audioData samples ended and audioGraph already stoped
            await _fileOutputNode.FinalizeAsync();

            // clean status and progress 
            status.Report("");
            _ioProgress.Report(0);

            return result;
        }


        private void FrameInputNode_QuantumStarted(
            AudioFrameInputNode sender,
            FrameInputNodeQuantumStartedEventArgs args)
        {
            if (_audioDataCurrentPosition == 0) _fileOutputNode.Start();

            // doesn't matter how many samples requested
            var frame = ProcessOutputFrame(_audioGraph.SamplesPerQuantum);
            _frameInputNode.AddFrame(frame);

            if (_finished)
            {
                _fileOutputNode?.Stop();
                _audioGraph?.Stop();
            }

            // to not report too many times
            if (_audioGraph == null)
                return;
            if (_audioGraph.CompletedQuantumCount % 100 == 0)
            {
                var dProgress =
                    (double) 100 *
                    _audioDataCurrentPosition /
                    _audioData.LengthSamples();
                _ioProgress?.Report(dProgress);
            }
        }

        private MediaEncodingProfile CreateMediaEncodingProfile(StorageFile file)
        {
            switch (file.FileType.ToLowerInvariant())
            {
                case ".wma":
                    return MediaEncodingProfile.CreateWma(AudioEncodingQuality.High);
                case ".mp3":
                    return MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
                case ".wav":
                    return MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
                default:
                    throw new ArgumentException(
                        "Can't create MediaEncodingProfile for this file extention");
            }
        }

        private unsafe AudioFrame ProcessOutputFrame(int requiredSamples)
        {
            var bufferSize = (uint) requiredSamples * sizeof(float) *
                             _fileOutputNode.EncodingProperties.ChannelCount;

            var frame = new AudioFrame(bufferSize);

            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (var reference = buffer.CreateReference())
            {
                // Get the buffer from the AudioFrame
                (reference as IMemoryBufferByteAccess).GetBuffer(
                    out var dataInBytes,
                    out var capacityInBytes);

                // Cast to float since the data we are generating is float
                var dataInFloat = (float*) dataInBytes;
                var capacityInFloat = capacityInBytes / sizeof(float);

                // Number of channels defines step between samples in buffer
                var channelCount = _fileOutputNode.EncodingProperties.ChannelCount;

                for (uint index = 0; index < capacityInFloat; index += channelCount)
                {
                    if (_audioDataCurrentPosition < _audioData.LengthSamples())
                    {
                        GetAudioData().SetCurrentChannelType(ChannelType.Left);
                        dataInFloat[index] = _audioData.GetOutputSample(
                            _audioDataCurrentPosition);
                    }

                    // if it's stereo
                    if (channelCount == 2)
                    {
                        // if processed audio is sretero
                        if (_audioData.IsStereo)
                        {
                            GetAudioData().SetCurrentChannelType(ChannelType.Right);
                            dataInFloat[index + 1] = _audioData.GetOutputSample(
                                _audioDataCurrentPosition);
                        }
                        else
                        {
                            // mute channel
                            dataInFloat[index + 1] = 0;
                        }
                    }

                    _audioDataCurrentPosition++;
                    if (_audioDataCurrentPosition >= _audioData.LengthSamples())
                    {
                        // last frame may be not full
                        _finished = true;
                        return frame;
                    }
                }
            }

            return frame;
        }*/
    }
} 