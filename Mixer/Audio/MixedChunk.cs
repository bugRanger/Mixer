namespace Mixer.Audio
{
    using System;
    using System.Collections.Generic;

    public class MixedChunk
    {
        #region Fields

        private readonly byte[] _mixture;
        private readonly byte[] _extract;

        private readonly HashSet<IAudioProvider> _providers;

        #endregion Fields

        #region Properties

        public AudioFormat Format { get; }

        #endregion Properties

        #region Constructors

        public MixedChunk(AudioFormat format) 
        {
            Format = format;

            _mixture = new byte[format.GetSamples()];
            _extract = new byte[format.GetSamples()];

            _providers = new HashSet<IAudioProvider>();
        }

        #endregion Constructors

        #region Methods

        public MixedChunk Build(params IAudioProvider[] providers)
        {
            for (int i = 0; i < providers.Length; i++)
            {
                if (_providers.Contains(providers[i]))
                {
                    continue;
                }

                int count = providers[i].Peak(out byte[] chunk);
                if (count > 0)
                {
                    Sum32BitAudio(_mixture, 0, chunk, count);
                    _providers.Add(providers[i]);
                }
            }

            return this;
        }

        public byte[] Exclude(IAudioProvider provider) 
        {
            if (!_providers.Contains(provider))
                return _mixture;

            int count = provider.Pull(out byte[] chunk);
            if (count == 0)
                return _mixture;

            Array.Copy(_mixture, _extract, _mixture.Length);

            Sub32BitAudio(_extract, 0, chunk, count);

            return _extract;
        }

        static unsafe void Sum32BitAudio(byte[] destBuffer, int offset, byte[] sourceBuffer, int bytesRead)
        {
            fixed (byte* pDestBuffer = &destBuffer[offset],
                      pSourceBuffer = &sourceBuffer[0])
            {
                float* pfDestBuffer = (float*)pDestBuffer;
                float* pfReadBuffer = (float*)pSourceBuffer;
                int samplesRead = bytesRead / 4;
                for (int n = 0; n < samplesRead; n++)
                {
                    pfDestBuffer[n] += pfReadBuffer[n];
                }
            }
        }

        static unsafe void Sub32BitAudio(byte[] destBuffer, int offset, byte[] sourceBuffer, int bytesRead)
        {
            fixed (byte* pDestBuffer = &destBuffer[offset],
                      pSourceBuffer = &sourceBuffer[0])
            {
                float* pfDestBuffer = (float*)pDestBuffer;
                float* pfReadBuffer = (float*)pSourceBuffer;
                int samplesRead = bytesRead / 4;
                for (int n = 0; n < samplesRead; n++)
                {
                    pfDestBuffer[n] -= pfReadBuffer[n];
                }
            }
        }

        #endregion Methods
    }
}
