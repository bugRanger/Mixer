namespace Mixer.Audio
{
    using System;
    using System.Threading;
    using System.Collections.Generic;
    
    public class MixingProvider : IDisposable
    {
        #region Fields

        private readonly object _locker;

        private readonly HashSet<IAudioProvider> _providers;
        private readonly AudioFormat _format;
        private readonly int _interval;

        private CancellationTokenSource _cancellation;
        private Worker<MixedChunk> _worker;
        private EventWaitHandle _waiter;
        private Thread _thread;

        private bool _disposed;

        #endregion Fields

        #region Constructors

        public MixingProvider(AudioFormat format)
            : this(format, format.Duration)
        {
        }

        public MixingProvider(AudioFormat format, int interval)
        {
            _locker = new object();
            _format = format;
            _interval = interval;

            _providers = new HashSet<IAudioProvider>();

            _cancellation = new CancellationTokenSource();
            _worker = new Worker<MixedChunk>(chunk => chunk.Unpack());
            _waiter = new EventWaitHandle(false, EventResetMode.AutoReset);
            _thread = new Thread(Handle);
            _thread.Priority = ThreadPriority.Highest;
            _thread.Start();
        }

        #endregion Constructors

        #region Methods

        public void Append(IAudioProvider provider)
        {
            lock (_locker)
            {
                _providers.Add(provider);
            }
        }

        public void Removed(IAudioProvider provider)
        {
            lock (_locker)
            {
                _providers.Remove(provider);
            }
        }

        public void WaitSync() 
        {
            _waiter.Set();
            _worker.WaitSync();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Handle() 
        {
            while (!_cancellation.IsCancellationRequested)
            {
                _waiter.WaitOne(_interval);

                lock (_locker)
                {
                    var chunk = new MixedChunk(_format);
                    chunk.Pack(_providers);

                    _worker.Enqueue(chunk);
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

                _waiter.Dispose();
                _waiter = null;

                _worker.Dispose();
                _worker = null;

                _cancellation.Dispose();
                _cancellation = null;

                _providers.Clear();
            }

            _disposed = true;
        }

        #endregion Methods
    }
}
