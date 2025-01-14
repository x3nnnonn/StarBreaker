namespace StarBreaker.Common;

//code from DotNext.IO

public sealed class StreamSegment : Stream
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly long _offset;
    private long _length;

    public StreamSegment(Stream stream, long offset, long length, bool leaveOpen = true)
    {
        _stream = stream;
        _leaveOpen = leaveOpen;
        _offset = offset;
        _length = length;
        _stream.Position = offset;
    }
    
    /// <inheritdoc/>
    public override bool CanRead => _stream.CanRead;

    /// <inheritdoc/>
    public override bool CanSeek => _stream.CanSeek;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => _length;

    /// <inheritdoc/>
    public override long Position
    {
        get => _stream.Position - _offset;
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)value, (ulong)_length, nameof(value));

            _stream.Position = _offset + value;
        }
    }

    private long RemainingBytes => _length - Position;

    /// <inheritdoc/>
    public override void Flush() => _stream.Flush();

    /// <inheritdoc/>
    public override Task FlushAsync(CancellationToken token) => _stream.FlushAsync(token);

    /// <inheritdoc/>
    public override bool CanTimeout => _stream.CanTimeout;

    /// <inheritdoc/>
    public override int ReadByte() => Position < _length ? _stream.ReadByte() : -1;

    /// <inheritdoc/>
    public override void WriteByte(byte value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);

        return _stream.Read(buffer, offset, (int)Math.Min(count, RemainingBytes));
    }

    /// <inheritdoc/>
    public override int Read(Span<byte> buffer) => _stream.Read(buffer.TrimLength(int.CreateSaturating(RemainingBytes)));

    /// <inheritdoc/>
    public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        count = (int)Math.Min(count, RemainingBytes);
        return _stream.BeginRead(buffer, offset, count, callback, state);
    }

    /// <inheritdoc/>
    public override int EndRead(IAsyncResult asyncResult) => _stream.EndRead(asyncResult);

    /// <inheritdoc/>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        => _stream.ReadAsync(buffer, offset, (int)Math.Min(count, RemainingBytes), token);

    /// <inheritdoc/>
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        => _stream.ReadAsync(buffer.TrimLength(int.CreateSaturating(RemainingBytes)), token);

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        if (newPosition < 0L)
            throw new IOException();

        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, _length, nameof(offset));

        Position = newPosition;
        return newPosition;
    }

    /// <inheritdoc/>
    public override void SetLength(long value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)value, (ulong)(_stream.Length - _stream.Position), nameof(value));

        _length = value;
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token = default) => Task.FromException(new NotSupportedException());

    /// <inheritdoc/>
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => ValueTask.FromException(new NotSupportedException());

    /// <inheritdoc/>
    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void EndWrite(IAsyncResult asyncResult) => throw new InvalidOperationException();

    /// <inheritdoc/>
    public override int ReadTimeout
    {
        get => _stream.ReadTimeout;
        set => _stream.ReadTimeout = value;
    }

    /// <inheritdoc/>
    public override int WriteTimeout
    {
        get => _stream.WriteTimeout;
        set => _stream.WriteTimeout = value;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
            _stream.Dispose();
        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
            await _stream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
}