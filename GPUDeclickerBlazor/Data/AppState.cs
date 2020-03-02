using GPUDeclickerUWP.Model.Data;
using System;

namespace GPUDeclickerBlazor.Data
{
    public class AppState
    {
        public AudioData AudioData { get; private set; }

        public event Action OnChange;

        public void SetAudioData(AudioData audioData)
        {
            AudioData = audioData;
            NotifyDataChanged();
        }

        private void NotifyDataChanged() => OnChange?.Invoke();
    }
}
