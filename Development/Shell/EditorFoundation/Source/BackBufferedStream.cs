// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.IO;

namespace Microsoft.TeamFoundation.Migration.Shell
{
    /// <summary>
    /// Buffers data already read from a stream. This allows backward seeking
    /// on streams that normally don't support seeking. Writing to the underlying
    /// stream is not supported.
    /// </summary>
    /// <remarks>
    /// The EditorFoundation uses a CryptoStream to hash the contents of a file
    /// while reading it. This provides a simple mechanism to determine whether
    /// the Model has changed and needs to be saved. However, the CryptoStream
    /// does not support seeking. The BackBufferedStream can be used by Model
    /// Serializers that require seeking back in the stream as part of the reading process.
    /// </remarks>
    public class BackBufferedStream : Stream
    {
        #region Fields
        private Stream stream;
        private Buffer buffer;
        private long streamPosition;
        private long bufferedPosition;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a new instance of a BackBufferedStream.
        /// </summary>
        /// <param name="stream">The stream to buffer.</param>
        /// <param name="bufferSize">The maximum size of the buffer.</param>
        public BackBufferedStream (Stream stream, int bufferSize)
        {
            if (stream == null)
            {
                throw new ArgumentNullException ("stream");
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException ("The specified stream can does not support reading", "stream");
            }

            this.stream = stream;
            this.buffer = new Buffer (bufferSize);
            this.streamPosition = 0;
            this.bufferedPosition = 0;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value><c>true</c> if the stream supports reading, <c>false</c> otherwise.</value>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value><c>true</c> if the stream supports seeking, <c>false</c> otherwise.</value>
        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value><c>false</c> always. The BackBufferedStream does not support writing.</value>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length
        {
            get
            {
                return this.stream.Length;
            }
        }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return this.bufferedPosition;
            }
            set
            {
                if (this.stream.CanSeek)
                {
                    this.stream.Position = value;
                    this.bufferedPosition = value;
                }
                else
                {
                    if (value > this.streamPosition)
                    {
                        throw new ArgumentException ("The position can only be set to a position less than the current position");
                    }
                    else if (value < 0)
                    {
                        throw new ArgumentException ("The position must be greater than or equal to zero");
                    }
                    else if (value < this.streamPosition)
                    {
                        if (this.streamPosition - value > this.buffer.Size)
                        {
                            throw new ArgumentOutOfRangeException ("The specified position exceeds the size of the buffer");
                        }
                        else
                        {
                            this.bufferedPosition = value;
                        }
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Clears all buffers for this stream.
        /// </summary>
        public override void Flush ()
        {
            this.stream.Flush ();
        }

        /// <summary>
        /// Sets the position within the current stream.
        /// </summary>
        /// <remarks>
        /// The BackBufferedStream's ability to seek to a particular location
        /// depends on the size of the buffer. For example, if the buffer size
        /// is 100 bytes, then the stream can only be seeked back at most 100 bytes.
        /// <para>Note: The BackBufferedStream only supports backward seeking.</para>
        /// </remarks>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek (long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                this.Position = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                this.Position += offset;
            }
            else if (origin == SeekOrigin.End)
            {
                this.Position = this.stream.Length - offset - 1;
            }

            return this.Position;
        }

        /// <summary>
        /// Sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength (long value)
        {
            this.stream.SetLength (value);
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read (byte[] buffer, int offset, int count)
        {
            if (buffer.Length < count)
            {
                throw new ArgumentException ("The specified buffer's length is less than the specified byte count to read");
            }

            int currentByte = 0;

            while (this.bufferedPosition < this.streamPosition)
            {
                buffer[currentByte] = this.buffer[this.streamPosition - this.bufferedPosition - 1];
                this.bufferedPosition++;
                currentByte ++;
            }

            int bytesRead = this.stream.Read (buffer, currentByte, count - currentByte);
            this.streamPosition += bytesRead;
            this.bufferedPosition += bytesRead;

            this.buffer.Push (buffer, currentByte, bytesRead);

            return currentByte + bytesRead;
        }

        /// <summary>
        /// Throws a NotSupportedException. The BackBufferedStream does not support writing.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write (byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException ("Write operations not supported");
        }
        #endregion

        #region Classes
        private class Buffer
        {
            #region Fields
            private byte[] buffer;
            private long head;
            private long size;
            #endregion

            #region Constructors
            public Buffer (long capacity)
            {
                if (capacity <= 0)
                {
                    throw new Exception ("Minimum capacity is one byte");
                }

                this.buffer = new byte[capacity];

                this.head = -1;
                this.size= 0;
            }
            #endregion

            #region Properties
            public long Size
            {
                get
                {
                    return this.size;
                }
            }
            #endregion

            #region Indexers
            public byte this[long offset]
            {
                get
                {
                    if (offset > this.Size)
                    {
                        throw new ArgumentException ("Requested offset must be less than the size of the buffer");
                    }
                    else if (this.Size == 0)
                    {
                        throw new InvalidOperationException ("Buffer is empty");
                    }

                    long index = this.DecrementIndex (this.head, offset);

                    return this.buffer[index];
                }
            }
            #endregion

            #region Public Methods
            public void Push (byte[] values)
            {
                this.Push (values, 0, values.Length);
            }

            public void Push (byte[] values, long startIndex, long length)
            {
                if (length > this.buffer.Length)
                {
                    startIndex += length - this.buffer.Length;
                    length = this.buffer.Length;
                }

                for (long i = 0; i < length; i++)
                {
                    this.Push (values[startIndex + i]);
                }
            }

            public void Push (byte value)
            {
                this.head = this.IncrementIndex (this.head);
                this.buffer[this.head] = value;
                this.size = Math.Min (this.size + 1, this.buffer.Length);
            }
            #endregion

            #region Private Methods
            private long IncrementIndex (long index)
            {
                return this.IncrementIndex (index, 1);
            }

            private long IncrementIndex (long index, long offset)
            {
                return index + offset == this.buffer.Length ? 0 : index + offset;
            }

            private long DecrementIndex (long index)
            {
                return this.DecrementIndex (index, 1);
            }

            private long DecrementIndex (long index, long offset)
            {
                return index - offset < 0 ? this.buffer.Length - 1 : index - offset;
            }
            #endregion
        }
        #endregion
    }
}
