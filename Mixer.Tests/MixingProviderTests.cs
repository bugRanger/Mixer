namespace Mixer.Tests
{
    using System;
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

        #endregion Fields

        #region Constructors

        [SetUp]
        public void Setup()
        {
            _format = new AudioFormat();
            _samples = new Dictionary<IAudioProvider, ArraySegment<byte>>();

            _mixer = new MixingProvider(_format, -1);
            _mixer.Append((_first = new Mock<IAudioProvider>()).Object);
            _mixer.Append((_second = new Mock<IAudioProvider>()).Object);
            _mixer.Append((_third = new Mock<IAudioProvider>()).Object);

            _first.Setup(s => s.Write(It.IsAny<ArraySegment<byte>>()))
                .Callback<ArraySegment<byte>>(samples => _samples[_first.Object] = samples);

            _second.Setup(s => s.Write(It.IsAny<ArraySegment<byte>>()))
                .Callback<ArraySegment<byte>>(samples => _samples[_second.Object] = samples);

            _third.Setup(s => s.Write(It.IsAny<ArraySegment<byte>>()))
                .Callback<ArraySegment<byte>>(samples => _samples[_third.Object] = samples);
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
            CollectionAssert.AreEqual(silence, _samples[_first.Object]);
            CollectionAssert.AreEqual(silence, _samples[_second.Object]);
            CollectionAssert.AreEqual(silence, _samples[_third.Object]);
        }

        [Test]
        public void Mixer_OnlyFirst_Samples()
        {
            // Arrange
            var silence = new byte[_format.GetSamples()];

            // Act
            _mixer.WaitSync();

            // Assert
            CollectionAssert.AreEqual(silence, _samples[_first.Object]);
            CollectionAssert.AreEqual(silence, _samples[_second.Object]);
            CollectionAssert.AreEqual(silence, _samples[_third.Object]);
        }

        #endregion Methods
    }
}