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

        private Dictionary<IAudioProvider, float[]> _samples;
        private Dictionary<IAudioProvider, float[]> _mixtures;

        #endregion Fields

        #region Constructors

        [SetUp]
        public void Setup()
        {
            _format = new AudioFormat();
            _samples = new Dictionary<IAudioProvider, float[]>();
            _mixtures = new Dictionary<IAudioProvider, float[]>();

            _mixer = new MixingProvider(_format, -1);
            _mixer.Append((_first = new Mock<IAudioProvider>()).Object);
            _mixer.Append((_second = new Mock<IAudioProvider>()).Object);
            _mixer.Append((_third = new Mock<IAudioProvider>()).Object);

            _first
                .Setup(s => s.Read(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<float[], int, int>((buffer, offest, count) => Read(_first.Object, buffer));
            _first
                .Setup(s => s.Write(It.IsAny<ArraySegment<float>>()))
                .Callback<ArraySegment<float>>(samples => Write(_first.Object, samples));

            _second
                .Setup(s => s.Read(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<float[], int, int>((buffer, offest, count) => Read(_second.Object, buffer));
            _second
                .Setup(s => s.Write(It.IsAny<ArraySegment<float>>()))
                .Callback<ArraySegment<float>>(samples => Write(_second.Object, samples));

            _third
                .Setup(s => s.Read(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<float[], int, int>((buffer, offest, count) => Read(_third.Object, buffer));
            _third
                .Setup(s => s.Write(It.IsAny<ArraySegment<float>>()))
                .Callback<ArraySegment<float>>(samples => Write(_third.Object, samples));
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

        [TestCase(0f, 0f, 0f)]
        [TestCase(0f, 0f, 3f)]
        [TestCase(0f, 2f, 0f)]
        [TestCase(0f, 2f, 3f)]
        [TestCase(1f, 0f, 0f)]
        [TestCase(1f, 2f, 3f)]
        [TestCase(1f, 1f, 3f)]
        [TestCase(1f, 1f, 1f)]
        [TestCase(0f, 1f, 1f)]
        [TestCase(1f, 1f, 0f)]
        public void Mixer_FromEach_MixedSamples(float first, float second, float third)
        {
            // Arrange
            var fromFirst = _samples[_first.Object] = Enumerable.Repeat(first, _format.GetSamples()).ToArray();
            var fromSecond = _samples[_second.Object] = Enumerable.Repeat(second, _format.GetSamples()).ToArray();
            var fromThird = _samples[_third.Object] = Enumerable.Repeat(third, _format.GetSamples()).ToArray();

            var toFirst = new float[_format.GetSamples()];
            MixedChunk.Sum32Bit(fromSecond, toFirst);
            MixedChunk.Sum32Bit(fromThird, toFirst);

            var toSecond = new float[_format.GetSamples()];
            MixedChunk.Sum32Bit(fromFirst, toSecond);
            MixedChunk.Sum32Bit(fromThird, toSecond);

            var toThird = new float[_format.GetSamples()];
            MixedChunk.Sum32Bit(fromFirst, toThird);
            MixedChunk.Sum32Bit(fromSecond, toThird);

            // Act
            _mixer.WaitSync();

            // Assert
            CollectionAssert.AreEqual(toFirst, _mixtures[_first.Object]);
            CollectionAssert.AreEqual(toSecond, _mixtures[_second.Object]);
            CollectionAssert.AreEqual(toThird, _mixtures[_third.Object]);
        }

        private int Read(IAudioProvider provider, float[] buffer)
        {
            if (!_samples.TryGetValue(provider, out var samples))
                return 0;

            Array.Copy(samples.ToArray(), buffer, buffer.Length);

            return buffer.Length;
        }

        private void Write(IAudioProvider provider, ArraySegment<float> buffer)
        {
            _mixtures[provider] = buffer.Array;
        }

        #endregion Methods
    }
}