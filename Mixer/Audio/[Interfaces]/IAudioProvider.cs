namespace Mixer.Audio
{
    using System;

    using NAudio.Wave;

    public interface IAudioProvider : ISampleProvider
    {
        #region Methods

        void Write(ArraySegment<float> samples);

        #endregion Methods
    }
}
