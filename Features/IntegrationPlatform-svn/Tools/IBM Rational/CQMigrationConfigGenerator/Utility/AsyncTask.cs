// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;


namespace Microsoft.TeamFoundation.Converters.Utility
{
    /// <summary>
    /// Summary description for Class1
    /// </summary>
    internal class AsyncTask<T>
    {
        private int m_noOfThreads;
        private ActivityQueue m_taskQueue;

        internal delegate bool DoTask(T taskItem);
        private static DoTask m_doTask;

        internal AsyncTask(int noOfThreads, DoTask TaskDelegate)
        {
            m_doTask = TaskDelegate;
            m_taskQueue = new ActivityQueue("AsyncTask Queue");
            m_noOfThreads = noOfThreads;
            StartThreads();
        }

        private void StartThreads()
        {
            for (int i = 0; i < m_noOfThreads; i++)
            {
                ThreadStart threadStart = new ThreadStart(PerformTask);
                Thread taskThread = ThreadManager.CreateThread(threadStart, false);
                taskThread.Name = "AsyncTask Thread" + i;
                taskThread.Start();
            }
        }

        internal void EnqueueTask(TaskInstance task)
        {
            //Logger.Write(LogSource.Common, TraceLevel.Verbose, "Task Enqueued");
            m_taskQueue.Enqueue(task);
        }

        internal void StopAsyncActivity()
        {
            //need to add more logic here
            m_taskQueue.WaitForMoreItems = false;
        }

        private void PerformTask()
        {
            while (m_taskQueue.WaitForMoreItems || m_taskQueue.Count > 0)
            {
                try
                {
                    TaskInstance taskInstance = (TaskInstance)m_taskQueue.Dequeue();
                    taskInstance.PerformTask();
                }
                catch (ConverterException)// this is the case where dequeue will throw an exception
                {
                }
            }
        }

        internal class TaskInstance
        {
            private T m_taskItem;
            private bool m_taskComplete;
            private bool m_errorInTask;

            internal TaskInstance()
            {
                m_errorInTask = false;
                m_taskComplete = false;
                m_taskItem = default(T);
            }

            internal TaskInstance(T taskItem)
            {
                m_errorInTask = false;
                m_taskComplete = false;
                m_taskItem = taskItem;
            }

            internal void SetTaskComplete()
            {
                lock (this)
                {
                    m_taskComplete = true;
                    Monitor.PulseAll(this);
                }
            }

            internal void WaitForTask()
            {
                lock (this)
                {
                    while (!m_taskComplete)
                    {
                        Monitor.Wait(this);
                    }
                }
            }

            internal bool TaskError
            {
                get { return m_errorInTask; }
            }

            internal T TaskItem
            {
                get { return m_taskItem; }
                set { m_taskItem = value; }
            }

            internal void PerformTask()
            {
                m_errorInTask = !AsyncTask<T>.m_doTask(m_taskItem);
                SetTaskComplete();
            }
        }

        class ActivityQueue
        {
            Queue m_activityQueue;
            bool m_flagWaitForMoreItems;
            string m_name;
            const int MAX_COUNT = int.MaxValue;

            internal ActivityQueue(string name)
            {
                m_activityQueue = new Queue();
                m_flagWaitForMoreItems = true;
                m_name = name;
            }

            internal void Enqueue(object item)
            {
                Logger.EnteredMethod(LogSource.Common, item);
                lock (m_activityQueue.SyncRoot)
                {
                    if (m_activityQueue.Count >= MAX_COUNT)
                    {
                        Logger.Write(LogSource.Common, TraceLevel.Warning,
                            "Reached max limit for queue: {0}, thread: {1}", m_name, Thread.CurrentThread.Name);
                        UtilityMethods.MonitorWait(m_activityQueue.SyncRoot);
                    }

                    m_activityQueue.Enqueue(item);

                    //send signal for the queue
                    Monitor.PulseAll(m_activityQueue.SyncRoot);
                }

                Logger.ExitingMethod(LogSource.Common);
            }

            internal object Dequeue()
            {
                Logger.EnteredMethod(LogSource.Common);
                bool isEmpty = true;
                object item = null;
                do
                {
                    lock (m_activityQueue.SyncRoot)
                    {
                        if (m_activityQueue.Count > 0)
                        {
                            item = m_activityQueue.Dequeue();
                            isEmpty = false;
                            if (m_activityQueue.Count >= MAX_COUNT - 1)
                            {
                                Logger.Write(LogSource.Common, TraceLevel.Warning,
                                    "Dequeue after max limit for queue: {0}, thread: {1}", m_name, Thread.CurrentThread.Name);
                                Monitor.Pulse(m_activityQueue.SyncRoot);
                            }
                        }
                        else
                        {
                            if (!m_flagWaitForMoreItems)
                            {
                                Logger.Write(LogSource.Common, TraceLevel.Info,
                                    "Throwing Exception to end thread: {0}", Thread.CurrentThread.Name);
                                throw new ConverterException();
                            }

                            Logger.Write(LogSource.Common, TraceLevel.Verbose,
                                "Waiting for thread: {0}", Thread.CurrentThread.Name);
                            UtilityMethods.MonitorWait(m_activityQueue.SyncRoot);
                            Logger.Write(LogSource.Common, TraceLevel.Verbose,
                                "Back for thread: {0}", Thread.CurrentThread.Name);
                        }
                    }

                } while (isEmpty);

                Logger.ExitingMethod(LogSource.Common, item);
                return item;
            }

            internal bool WaitForMoreItems
            {
                get { return m_flagWaitForMoreItems; }
                set
                {
                    lock (m_activityQueue.SyncRoot)
                    {
                        m_flagWaitForMoreItems = value;
                        Monitor.PulseAll(m_activityQueue.SyncRoot);
                    }
                }
            }

            internal int Count
            {
                get { return m_activityQueue.Count; }
            }
        }
    }
}
