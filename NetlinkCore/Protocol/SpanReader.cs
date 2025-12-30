using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetlinkCore.Protocol;

internal unsafe ref struct SpanReader(ReadOnlySpan<byte> buffer)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    private int _position = 0;

    public readonly bool IsEndOfBuffer => _position == _buffer.Length;

    public ReadOnlySpan<byte> Read(int length)
    {
        var slice = _buffer.Slice(_position, length);
        _position += Alignment.Align(length);
        return slice;
    }

    public ref readonly T Read<T>() where T : unmanaged => ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(Read(sizeof(T))));
}