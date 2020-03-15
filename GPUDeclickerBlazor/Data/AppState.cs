using GPUDeclickerUWP.Model.Data;
using System;

namespace GPUDeclickerBlazor.Data
{
    public class AppState
    {
        const float _defaultThreshold = 10f;
        const int _defaultMaxLength = 250;

        // Audio processing parameters
        public float Threshold
        {
            get
            {
                return AudioData is null 
                    ? _defaultThreshold 
                    : AudioData.AudioProcessingSettings.ThresholdForDetection;
            }
            set
            {
                if (AudioData != null)
                    AudioData.AudioProcessingSettings.ThresholdForDetection = value;
            }
        }

        public int MaxLength
        {
            get
            {
                return AudioData is null
                    ? _defaultMaxLength
                    : AudioData.AudioProcessingSettings.MaxLengthOfCorrection;
            }
            set
            {
                if (AudioData != null)
                    AudioData.AudioProcessingSettings.MaxLengthOfCorrection = value;
            }
        }

        public AudioData AudioData { get; private set; }

        public event Action OnAudioChange;

        public void SetAudioData(AudioData audioData)
        {
            if (audioData is null)
                return;

            AudioData = audioData;
            AudioData.AudioProcessingSettings.ThresholdForDetection = _defaultThreshold;
            AudioData.AudioProcessingSettings.MaxLengthOfCorrection = _defaultMaxLength;
            NotifyAudioDataChanged();
        }

        private void NotifyAudioDataChanged() => OnAudioChange?.Invoke();
    }
}
