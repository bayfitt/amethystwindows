using System;
using System.Threading;

namespace AmethystWindows
{
    // One manager per Windows session; a second launch asks the first to open settings.
    internal sealed class SingleInstance : IDisposable
    {
        private readonly EventWaitHandle activationEvent;
        private readonly Mutex mutex;
        private RegisteredWaitHandle? activationWait;
        private bool disposed;

        internal bool IsPrimary { get; }

        internal SingleInstance(string name)
        {
            // Create the event first so a second launch can signal even during startup.
            activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, $@"Local\{name}.Activate");
            mutex = new Mutex(false, $@"Local\{name}.Mutex");
            try
            {
                IsPrimary = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                IsPrimary = true;
            }
        }

        internal void Listen(Action onActivation)
        {
            if (!IsPrimary || activationWait != null)
                throw new InvalidOperationException("Only the primary instance can listen once.");

            activationWait = ThreadPool.RegisterWaitForSingleObject(
                activationEvent, (_, _) => onActivation(), null, Timeout.Infinite, false);
        }

        internal void RequestActivation() => activationEvent.Set();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            activationWait?.Unregister(null);
            activationEvent.Dispose();
            if (IsPrimary) mutex.ReleaseMutex();
            mutex.Dispose();
        }
    }
}
