// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Migration.Toolkit.WCFServices
{
    // Based on Stephen Toub's BlockingStream (MSDN Magazine, 2/08)

    internal class MemoryQueueStream : Stream
    {
        private object m_readLock;
        private object m_mutexLock;
        private Queue<byte[]> m_chunks;
        private byte[] m_currentChunk;
        private int m_currentChunkPosition;
        private int m_queueSizeBytes;

        /// <summary>
        /// This is an arbitrary max size of the queue in bytes.
        /// Should be large enough to hold all of the trace information that might be built
        /// up during the course of normal operation before a client has a chance to poll
        /// and drain the queue.
        /// </summary>
        private const int MaxQueueSizeBytes = 10000000;
        private volatile bool m_illegalToWrite;

        public MemoryQueueStream()
        {
            m_chunks = new Queue<byte[]>();
            m_queueSizeBytes = 0;
            m_readLock = new object();
            m_mutexLock = new object();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (count == 0)
            {
                return 0;
            }

            while (true)
            {
                lock (m_readLock)
                {
                    lock (m_mutexLock)
                    {
                        if (m_currentChunk == null)
                        {
                            if (m_chunks.Count == 0)
                            {
                                return 0;
                            }
                            m_currentChunk = m_chunks.Dequeue();
                            m_queueSizeBytes -= m_currentChunk.Length;
                            Debug.Assert(m_queueSizeBytes >= 0, "Queue size should be >= 0, is: " + m_queueSizeBytes);
                            m_currentChunkPosition = 0;
                        }
                    }

                    int bytesAvailable = m_currentChunk.Length - m_currentChunkPosition;
                    int bytesToCopy;

                    if (bytesAvailable > count)
                    {
                        bytesToCopy = count;
                        Buffer.BlockCopy(m_currentChunk, m_currentChunkPosition, buffer, offset, count);
                        m_currentChunkPosition += count;
                    }
                    else
                    {
                        bytesToCopy = bytesAvailable;

                        Buffer.BlockCopy(m_currentChunk, m_currentChunkPosition, buffer, offset, bytesToCopy);
                        m_currentChunk = null;
                        m_currentChunkPosition = 0;
                    }
                    return bytesToCopy;
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            if (count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (count == 0)
            {
                return;
            }

            byte[] chunk = new byte[count];
            Buffer.BlockCopy(buffer, offset, chunk, 0, count);
            
            lock (m_mutexLock)
            {
                if (m_illegalToWrite)
                {
                    throw new InvalidOperationException("Writing has already been completed.");
                }

                // only write our new bytes if the size of the updated
                // queue is less than the max queue size.
                int newSizeBytes = m_queueSizeBytes + chunk.Length;
                if (newSizeBytes <= MaxQueueSizeBytes)
                {
                    m_queueSizeBytes = newSizeBytes;
                    m_chunks.Enqueue(chunk);
                }
            }
        }

        public override bool CanRead 
        { 
            get { return true; } 
        }
        
        public override bool CanSeek 
        { 
            get { return false; } 
        }

        public override bool CanWrite 
        { 
            get { return !m_illegalToWrite; } 
        }

        public override void Flush() 
        {
            return;
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }
        
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public void SetEndOfStream()
        {
            lock (m_mutexLock)
            {
                m_illegalToWrite = true;
            }
        }
    }
}