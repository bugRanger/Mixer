namespace Mixer.Audio
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public class Worker<T> : IDisposable
    {
        #region Fields

        private readonly ConcurrentQueue<T> _queue;
        private readonly Action<T> _action;

        private CancellationTokenSource _cancellation;
        private EventWaitHandle _waiter;
        private Thread _thread;

        private bool _disposed;

        #endregion Fields

        #region Constructors

        public Worker(Action<T> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _queue = new ConcurrentQueue<T>();

            _cancellation = new CancellationTokenSource();
            _waiter = new EventWaitHandle(false, EventResetMode.AutoReset);
            _thread = new Thread(HandleAction);
            _thread.Priority = ThreadPriority.Highest;
            _thread.Start();
        }

        #endregion Constructors

        #region Methods

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
        }

        public void WaitSync() 
        {
            _waiter.WaitOne();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

                _cancellation.Dispose();
                _cancellation = null;
            }

            _disposed = true;
        }

        private void HandleAction()
        {
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    var handle = false;

                    while (_queue.TryDequeue(out var item))
                    {
                        handle = true;
                        _action.Invoke(item);
                    }

                    if (handle)
                        _waiter.Set();
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
        }

        #endregion Methods
    }
}
