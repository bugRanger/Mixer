namespace Mixer.Tests
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Moq;
    using NUnit.Framework;

    using Mixer.Audio;

    [TestFixture]
    public class MixingProviderTests
    {
        #region Fields

        private AudioFormat _format;
        private MixingProvider _mixer;
        private Mock<IAudioProvider> _first;
        private Mock<IAudioProvider> _second;
        private Mock<IAudioProvider> _third;

        private Dictionary<IAudioProvider, ArraySegment<byte>> _samples;
        private Dictionary<IAudioProvider, ArraySegment<byte>> _mixtures;

        #endregion Fields

        #region Constructors

        [SetUp]
        public void Setup()
        {
            _format = new AudioFormat();
            _samples = new Dictionary<IAudioProvider, ArraySegment<byte>>();
            _mixtures = new Dictionary<IAudioProvider, ArraySegment<byte>>();

            _mixer = new MixingProvider(_format, -1);
            _mixer.Append((_first = new Mock<IAudioProvider>()).Object);
            _mixer.Append((_second = new Mock<IAudioProvider>()).Object);
            _mixer.Append((_third = new Mock<IAudioProvider>()).Object);

            _first
                .Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((buffer, offest, count) => Read(_first.Object, buffer));
            _first
                .Setup(s => s.Write(It.IsAny<ArraySegment<byte>>()))
                .Callback<ArraySegment<byte>>(samples => Write(_first.Object, samples));

            _second
                .Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((buffer, offest, count) => Read(_second.Object, buffer));
            _second
                .Setup(s => s.Write(It.IsAny<ArraySegment<byte>>()))
                .Callback<ArraySegment<byte>>(samples => Write(_second.Object, samples));

            _third
                .Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[], int, int>((buffer, offest, count) => Read(_third.Object, buffer));
            _third
                .Setup(s => s.Write(It.IsAny<ArraySegment<byte>>()))
                .Callback<ArraySegment<byte>>(samples => Write(_third.Object, samples));
        }

        #endregion Constructors

        #region Methods

        [Test]
        public void Mixer_Empty_Silence()
        {
            // Arrange
            var silence = new byte[_format.GetSamples()];

            // Act
            _mixer.WaitSync();

            // Assert
            CollectionAssert.AreEqual(silence, _mixtures[_first.Object]);
            CollectionAssert.AreEqual(silence, _mixtures[_second.Object]);
            CollectionAssert.AreEqual(silence, _mixtures[_third.Object]);
        }

        [Test]
        public void Mixer_FromEach_MixedSamples()
        {
            // Arrange
            var fromFirst = _samples[_first.Object] = Enumerable.Repeat<byte>(1, _format.GetSamples()).ToArray();
            var fromSecond = _samples[_second.Object] = Enumerable.Repeat<byte>(2, _format.GetSamples()).ToArray();
            var fromThird = _samples[_third.Object] = Enumerable.Repeat<byte>(3, _format.GetSamples()).ToArray();

            var toFirst = new byte[_format.GetSamples()];
            MixedChunk.Sum32Bit(fromSecond, toFirst);
            MixedChunk.Sum32Bit(fromThird, toFirst);

            var toSecond = new byte[_format.GetSamples()];
            MixedChunk.Sum32Bit(fromFirst, toSecond);
            MixedChunk.Sum32Bit(fromThird, toSecond);

            var toThird = new byte[_format.GetSamples()];
            MixedChunk.Sum32Bit(fromFirst, toThird);
            MixedChunk.Sum32Bit(fromSecond, toThird);

            // Act
            _mixer.WaitSync();

            // Assert
            CollectionAssert.AreEqual(toFirst, _mixtures[_first.Object]);
            CollectionAssert.AreEqual(toSecond, _mixtures[_second.Object]);
            CollectionAssert.AreEqual(toThird, _mixtures[_third.Object]);
        }

        private int Read(IAudioProvider provider, byte[] buffer)
        {
            if (!_samples.TryGetValue(provider, out var samples))
                return 0;

            Array.Copy(samples.ToArray(), buffer, buffer.Length);

            return buffer.Length;
        }

        private void Write(IAudioProvider provider, ArraySegment<byte> buffer)
        {
            _mixtures[provider] = buffer;
        }

        #endregion Methods
    }
}