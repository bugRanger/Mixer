using System;

namespace Mixer.Audio
{
    using System;

    using NAudio.Wave;

    // TODO: Move to float array.
    public interface IAudioProvider : IWaveProvider
    {
        #region Methods

        // TODO: Move to float array.
        void Write(ArraySegment<byte> samples);

        #endregion Methods
    }
}
