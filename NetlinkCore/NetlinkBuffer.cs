using System;
using System.Buffers;

namespace NetlinkCore;

internal readonly struct NetlinkBuffer(NetlinkBufferSize size) : IDisposable
{
    private readonly byte[] _buffer = ArrayPool<byte>.Shared.Rent((int)size);

    public void Dispose() => ArrayPool<byte>.Shared.Return(_buffer);

    public static implicit operator Span<byte>(NetlinkBuffer buffer) => buffer._buffer;
}