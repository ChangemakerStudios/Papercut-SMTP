﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
// https://raw.githubusercontent.com/aspnet/Hosting/rel/1.1.2/src/Microsoft.AspNetCore.TestHost/ResponseStream.cs


using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace Papercut.Service.Web.Hosting.InProcess;

// This steam accepts writes from the server/app, buffers them internally, and returns the data via Reads
// when requested by the client.
internal class ResponseStream : Stream
{
    private bool _complete;
    private bool _aborted;
    private Exception _abortException;
    private ConcurrentQueue<byte[]> _bufferedData;
    private ArraySegment<byte> _topBuffer;
    private SemaphoreSlim _readLock;
    private SemaphoreSlim _writeLock;
    private TaskCompletionSource<object> _readWaitingForData;
    private object _signalReadLock;

    private Func<Task> _onFirstWriteAsync;
    private bool _firstWrite;
    private Action _abortRequest;

    internal ResponseStream(Func<Task> onFirstWriteAsync, Action abortRequest)
    {
            if (onFirstWriteAsync == null)
            {
                throw new ArgumentNullException(nameof(onFirstWriteAsync));
            }

            if (abortRequest == null)
            {
                throw new ArgumentNullException(nameof(abortRequest));
            }

            _onFirstWriteAsync = onFirstWriteAsync;
            _firstWrite = true;
            _abortRequest = abortRequest;

            _readLock = new SemaphoreSlim(1, 1);
            _writeLock = new SemaphoreSlim(1, 1);
            _bufferedData = new ConcurrentQueue<byte[]>();
            _readWaitingForData = new TaskCompletionSource<object>();
            _signalReadLock = new object();
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
        get { return true; }
    }

    #region NotSupported

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

    #endregion NotSupported

    public override void Flush()
    {
            CheckNotComplete();

            _writeLock.Wait();
            try
            {
                FirstWriteAsync().GetAwaiter().GetResult();
            }
            finally
            {
                _writeLock.Release();
            }

            // TODO: Wait for data to drain?
        }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
            if (cancellationToken.IsCancellationRequested)
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            Flush();

            // TODO: Wait for data to drain?

            return Task.FromResult<object>(null);
        }

    public override int Read(byte[] buffer, int offset, int count)
    {
            VerifyBuffer(buffer, offset, count, allowEmpty: false);
            _readLock.Wait();
            try
            {
                int totalRead = 0;
                do
                {
                    // Don't drain buffered data when signaling an abort.
                    CheckAborted();
                    if (_topBuffer.Count <= 0)
                    {
                        byte[] topBuffer = null;
                        while (!_bufferedData.TryDequeue(out topBuffer))
                        {
                            if (_complete)
                            {
                                CheckAborted();
                                // Graceful close
                                return totalRead;
                            }
                            WaitForDataAsync().Wait();
                        }
                        _topBuffer = new ArraySegment<byte>(topBuffer);
                    }
                    int actualCount = Math.Min(count, _topBuffer.Count);
                    Buffer.BlockCopy(_topBuffer.Array, _topBuffer.Offset, buffer, offset, actualCount);
                    _topBuffer = new ArraySegment<byte>(_topBuffer.Array,
                        _topBuffer.Offset + actualCount,
                        _topBuffer.Count - actualCount);
                    totalRead += actualCount;
                    offset += actualCount;
                    count -= actualCount;
                }
                while (count > 0 && (_topBuffer.Count > 0 || _bufferedData.Count > 0));
                // Keep reading while there is more data available and we have more space to put it in.
                return totalRead;
            }
            finally
            {
                _readLock.Release();
            }
        }

    public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        VerifyBuffer(buffer, offset, count, allowEmpty: false);
        CancellationTokenRegistration registration = cancellationToken.Register(Abort);
        await _readLock.WaitAsync(cancellationToken);
        try
        {
            int totalRead = 0;
            do
            {
                // Don't drained buffered data on abort.
                CheckAborted();
                if (_topBuffer.Count <= 0)
                {
                    byte[] topBuffer = null;
                    while (!_bufferedData.TryDequeue(out topBuffer))
                    {
                        if (_complete)
                        {
                            CheckAborted();
                            // Graceful close
                            return totalRead;
                        }
                        await WaitForDataAsync();
                    }
                    _topBuffer = new ArraySegment<byte>(topBuffer);
                }
                int actualCount = Math.Min(count, _topBuffer.Count);
                Buffer.BlockCopy(_topBuffer.Array, _topBuffer.Offset, buffer, offset, actualCount);
                _topBuffer = new ArraySegment<byte>(_topBuffer.Array,
                    _topBuffer.Offset + actualCount,
                    _topBuffer.Count - actualCount);
                totalRead += actualCount;
                offset += actualCount;
                count -= actualCount;
            }
            while (count > 0 && (_topBuffer.Count > 0 || _bufferedData.Count > 0));
            // Keep reading while there is more data available and we have more space to put it in.
            return totalRead;
        }
        finally
        {
            registration.Dispose();
            _readLock.Release();
        }
    }

    // Called under write-lock.
    private Task FirstWriteAsync()
    {
            if (_firstWrite)
            {
                _firstWrite = false;
                return _onFirstWriteAsync();
            }
            return Task.FromResult(true);
        }

    // Write with count 0 will still trigger OnFirstWrite
    public override void Write(byte[] buffer, int offset, int count)
    {
            VerifyBuffer(buffer, offset, count, allowEmpty: true);
            CheckNotComplete();

            _writeLock.Wait();
            try
            {
                FirstWriteAsync().GetAwaiter().GetResult();
                if (count == 0)
                {
                    return;
                }
                // Copies are necessary because we don't know what the caller is going to do with the buffer afterwards.
                byte[] internalBuffer = new byte[count];
                Buffer.BlockCopy(buffer, offset, internalBuffer, 0, count);
                _bufferedData.Enqueue(internalBuffer);

                SignalDataAvailable();
            }
            finally
            {
                _writeLock.Release();
            }
        }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
            VerifyBuffer(buffer, offset, count, allowEmpty: true);
            if (cancellationToken.IsCancellationRequested)
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            Write(buffer, offset, count);
            return Task.FromResult<object>(null);
        }

    private static void VerifyBuffer(byte[] buffer, int offset, int count, bool allowEmpty)
    {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset", offset, string.Empty);
            }
            if (count < 0 || count > buffer.Length - offset
                || (!allowEmpty && count == 0))
            {
                throw new ArgumentOutOfRangeException("count", count, string.Empty);
            }
        }

    private void SignalDataAvailable()
    {
            // Dispatch, as TrySetResult will synchronously execute the waiters callback and block our Write.
            Task.Factory.StartNew(() => _readWaitingForData.TrySetResult(null));
        }

    private Task WaitForDataAsync()
    {
            // Prevent race with Dispose
            lock (_signalReadLock)
            {
                _readWaitingForData = new TaskCompletionSource<object>();

                if (!_bufferedData.IsEmpty || _complete)
                {
                    // Race, data could have arrived before we created the TCS.
                    _readWaitingForData.TrySetResult(null);
                }

                return _readWaitingForData.Task;
            }
        }

    internal void Abort()
    {
            Abort(new OperationCanceledException());
        }

    internal void Abort(Exception innerException)
    {
            Contract.Requires(innerException != null);
            _aborted = true;
            _abortException = innerException;
            Complete();
        }

    internal void Complete()
    {
            // If HttpClient.Dispose gets called while HttpClient.SetTask...() is called
            // there is a chance that this method will be called twice and hang on the lock
            // to prevent this we can check if there is already a thread inside the lock
            if (_complete)
            {
                return;
            }

            // Prevent race with WaitForDataAsync
            lock (_signalReadLock)
            {
                // Throw for further writes, but not reads.  Allow reads to drain the buffered data and then return 0 for further reads.
                _complete = true;
                _readWaitingForData.TrySetResult(null);
            }
        }

    private void CheckAborted()
    {
            if (_aborted)
            {
                throw new IOException(string.Empty, _abortException);
            }
        }

    protected override void Dispose(bool disposing)
    {
            if (disposing)
            {
                _abortRequest();
            }
            base.Dispose(disposing);
        }

    private void CheckNotComplete()
    {
            if (_complete)
            {
                throw new IOException("The request was aborted or the pipeline has finished");
            }
        }
}