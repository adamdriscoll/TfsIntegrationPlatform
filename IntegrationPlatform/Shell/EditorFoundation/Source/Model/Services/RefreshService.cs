// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Threading;
using System.Windows.Threading;

namespace Microsoft.TeamFoundation.Migration.Shell.Model
{
    class RefreshService : IRefreshService
    {
        private Dispatcher m_dispatcher;
        private Thread m_workerThread;
        private int m_refreshIntervalMilliseconds;
        
        public RefreshService(Dispatcher dispatcher, int refreshIntervalSeconds)
        {
            m_dispatcher = dispatcher;
            
            StopRequested = true; // start service in paused state

            m_workerThread = new Thread(new ThreadStart(Start));
            m_workerThread.IsBackground = true;

            RefreshIntervalMilliseconds = refreshIntervalSeconds * 1000;

            m_workerThread.Start();
        }

        public int RefreshIntervalMilliseconds
        {
            get
            {
                return m_refreshIntervalMilliseconds;
            }
            set
            {
                if (m_refreshIntervalMilliseconds != value)
                {
                    m_refreshIntervalMilliseconds = value;
                    m_workerThread.Interrupt();
                }
            }
        }

        public void ForceRefresh()
        {
            if (AutoRefresh != null)
            {
                FireOnUIThread(AutoRefresh);
            }
        }

        private void Start()
        {
            if (RefreshStart != null)
            {
                RefreshStart(this, EventArgs.Empty);
            }

            while (true)
            {
                try
                {
                    if (!StopRequested && AutoRefresh != null && RefreshIntervalMilliseconds > 0)
                    {
                        // Calling update on background thread and relying upon InvokeOC seems
                        // to be thousands of times slower than just pinning the UI thread with 
                        // the work to fetch data.  That is what we do here... use the dispatch queue
                        // get to the UI thread.

                        FireOnUIThread(AutoRefresh);
                    }

                    Thread.Sleep(RefreshIntervalMilliseconds > 0 ? RefreshIntervalMilliseconds : Timeout.Infinite);
                }
                catch (ThreadInterruptedException)
                {
                }
            }
        }

        public void Pause()
        {
            StopRequested = true;

            if (RefreshPause != null)
            {
                FireOnUIThread(RefreshPause);
            }
        }

        public void Resume()
        {
            StopRequested = false;

            if (RefreshResume != null)
            {
                FireOnUIThread(RefreshResume);
            }
        }

        public event EventHandler AutoRefresh;
        public event EventHandler RefreshStart;
        public event EventHandler RefreshPause;
        public event EventHandler RefreshResume;

        private void FireOnUIThread(EventHandler handler)
        {
            if (m_dispatcher.CheckAccess())
            {
                handler(this, EventArgs.Empty);
            }
            else
            {
                m_dispatcher.Invoke(handler, DispatcherPriority.Send, new object[] { this, EventArgs.Empty });
            }
        }

        private bool m_stopRequested;
        private object m_lock = new object();

        private bool StopRequested
        {
            get
            {
                lock (m_lock)
                {
                    return m_stopRequested;
                }
            }
            set
            {
                lock (m_lock)
                {
                    m_stopRequested = value;
                }
            }
        }
    }
}
