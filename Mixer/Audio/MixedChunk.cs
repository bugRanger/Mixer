namespace Mixer.Audio
{
    using System;
    using System.Collections.Generic;

    public class MixedChunk
    {
        #region Fields

        private readonly Dictionary<IAudioProvider, ArraySegment<byte>> _providerSamples;
        private readonly byte[] _mixture;

        #endregion Fields

        #region Properties

        public AudioFormat Format { get; }

        #endregion Properties

        #region Constructors

        public MixedChunk(AudioFormat format) 
        {
            Format = format;

            _mixture = new byte[format.GetSamples()];
            _providerSamples = new Dictionary<IAudioProvider, ArraySegment<byte>>();
        }

        #endregion Constructors

        #region Methods

        public void Pack(IEnumerable<IAudioProvider> providers)
        {
            foreach (var provider in providers)
            {
                if (_providerSamples.ContainsKey(provider))
                {
                    return;
                }

                var samples = new byte[_mixture.Length];

                int count = provider.Read(samples, 0, samples.Length);
                if (count != 0)
                {
                    Sum32Bit(samples, _mixture);

                    _providerSamples[provider] = samples;
                }
                else
                {
                    _providerSamples[provider] = _mixture;
                }
            }
        }

        public void Unpack()
        {
            foreach (var pair in _providerSamples)
            {
                IAudioProvider provider = pair.Key;
                ArraySegment<byte> samples = pair.Value;

                if (!ReferenceEquals(_mixture, samples))
                {
                    Sub32Bit(_mixture, samples);
                }

                provider.Write(samples);
            }
        }

        [Obsolete("Remove: use safe methods.")]
        internal static unsafe void Sum32Bit(ArraySegment<byte> source, byte[] dest)
        {
            fixed (byte* pSource = &source.Array[0], pDest = &dest[0])
            {
                int count = (source.Count - source.Offset) / 4;

                float* pfSource = (float*)pSource;
                float* pfDest = (float*)pDest;

                for (int n = 0; n < count; n++)
                {
                    pfDest[n] += pfSource[n];
                }
            }
        }

        [Obsolete("Remove: use safe methods.")]
        internal static unsafe void Sub32Bit(byte[] source, ArraySegment<byte> dest)
        {
            fixed (byte* pSource = &source[0], pDest = &dest.Array[0])
            {
                int count = (dest.Count - dest.Offset) / 4;

                float* pfSource = (float*)pSource;
                float* pfDest = (float*)pDest;

                for (int n = 0; n < count; n++)
                {
                    pfDest[n] = pfSource[n] - pfDest[n];
                }
            }
        }

        #endregion Methods
    }
}
