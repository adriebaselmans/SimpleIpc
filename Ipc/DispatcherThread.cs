using System;
using System.Threading;
using System.Windows.Threading;

namespace Ipc
{
    public class DispatcherThread : IDisposable
    {
        private readonly Thread _dispatcherThread;
        private readonly Delegate _threadStart;
        private readonly AutoResetEvent _threadStarted = new AutoResetEvent(false);

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="threadStart">the method to run. Must contain Dispatcher.Run() in its body.</param>
        public DispatcherThread(ThreadStart threadStart)
        {
            _threadStart = threadStart;
             _dispatcherThread = new Thread(DispatcherStart) { IsBackground = true };
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="threadStart">the method to run. Must contain Dispatcher.Run() in its body.</param>
        public DispatcherThread(ParameterizedThreadStart threadStart)
        {
            _threadStart = threadStart;
            _dispatcherThread = new Thread(ParameterizedDispatcherStart) { IsBackground = true };
        }

        /// <summary>
        ///     Set thread name.
        /// </summary>
        public string Name
        {
            get { return _dispatcherThread.Name; }
            set { _dispatcherThread.Name = value; }
        }

        /// <summary>
        ///     Set thread priority.
        /// </summary>
        public ThreadPriority Priority
        {
            set { _dispatcherThread.Priority = value; }
        }

        /// <summary>
        ///     Gets the dispatcher if the thread was started, else null.
        /// </summary>
        public Dispatcher Dispatcher { get; private set; }

        /// <summary>
        ///     Start the dispatcher thread. Blocks until Dispatcher is available. Note: This is not the same as Block until
        ///     Dispatcher.Run is called
        ///     This method returns when the MessagePump is created (so events can be beginInvoked on it), but reading from the
        ///     messagePump (with Dispatcher.Run)
        ///     is probably not yet executed
        /// </summary>
        public void Start()
        {
            _dispatcherThread.Start();
            _threadStarted.WaitOne();
        }

        /// <summary>
        ///     Start the dispatcher thread. Blocks until Dispatcher is available.  Note: This is not the same as Block until
        ///     Dispatcher.Run is called
        ///     This method returns when the MessagePump is created (so events can be beginInvoked on it), but reading from the
        ///     messagePump (with Dispatcher.Run)
        ///     is probably not yet executed
        /// </summary>
        /// <param name="parameter">single parameter passed with the thread start.</param>
        public void Start(object parameter)
        {
            _dispatcherThread.Start(parameter);
            _threadStarted.WaitOne();
        }

        /// <summary>
        ///     Start the dispatcher thread. Blocks until Dispatcher is available.  Note: This is not the same as Block until
        ///     Dispatcher.Run is called
        ///     This method returns when the MessagePump is created (so events can be beginInvoked on it), but reading from the
        ///     messagePump (with Dispatcher.Run)
        ///     is probably not yet executed
        /// </summary>
        /// <param name="parameters">multiple parameters passed with the thread start.</param>
        public void Start(params object[] parameters)
        {
            _dispatcherThread.Start(parameters);
            _threadStarted.WaitOne();
        }

        /// <summary>
        ///     Joins the dispatcher thread.
        /// </summary>
        public void Join()
        {
            if (Dispatcher != null)
            {
                _dispatcherThread.Join();
                Dispatcher = null;
            }
        }

        /// <summary>
        ///     Shuts down the dispatcher thread.
        /// </summary>
        public void Shutdown()
        {
            if (Dispatcher != null)
            {
                Dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                Dispatcher = null;
                _dispatcherThread.Join();
            }
        }

        /// <summary>
        ///     Forcefully shuts down the thread. No dispatcher shutdown call is made.
        /// </summary>
        public void Abort()
        {
            _dispatcherThread.Abort();
        }

        private void DispatcherStart()
        {
            InitializeThreadStart();
            _threadStart.DynamicInvoke();
        }

        private void ParameterizedDispatcherStart(object parameter)
        {
            InitializeThreadStart();
            _threadStart.DynamicInvoke(parameter);
        }

        private void InitializeThreadStart()
        {
            Dispatcher = Dispatcher.CurrentDispatcher;
            _threadStarted.Set();
        }

        #region IDisposable Support
        private bool _disposed = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _threadStarted.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}