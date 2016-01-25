using System;

namespace Discovery
{
    public abstract class Disposable : IDisposable
    {
        protected bool IsDisposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Disposable()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }
    }
}