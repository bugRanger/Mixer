using System;

namespace Mixer.Audio
{
    using System;

    using NAudio.Wave;

    public interface IAudioProvider : IWaveProvider
    {
        #region Methods

        void Write(ArraySegment<byte> samples);

        #endregion Methods
    }
}
