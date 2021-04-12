﻿namespace Mixer.Audio
{
    using System;
    using System.Threading;

    public class MixingProvider : IDisposable
    {
        #region Fields

        private readonly IAudioProvider[] _providers;
        private readonly AudioFormat _format;

        private CancellationTokenSource _cancellation;
        private Thread _thread;

        private bool _disposed;

        #endregion Fields

        #region Events

        public event Action<IAudioProvider, byte[]> Completed;

        #endregion Events

        #region Constructors

        public MixingProvider(IAudioProvider[] providers, AudioFormat format)
        {
            _providers = providers;
            _format = format;

            _cancellation = new CancellationTokenSource();
            _thread = new Thread(Mixing);
            _thread.Start();
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Mixing() 
        {
            while (!_cancellation.IsCancellationRequested)
            {
                Thread.Sleep(_format.Duration);

                var mixedChunk = 
                    new MixedChunk(_format)
                    .Build(_providers);

                for (int i = 0; i < _providers.Length; i++)
                {
                    Completed?.Invoke(_providers[i], mixedChunk.Exclude(_providers[i]));
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _cancellation.Cancel();
                _thread.Join();
                _thread = null;

                _cancellation.Dispose();
                _cancellation = null;
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
