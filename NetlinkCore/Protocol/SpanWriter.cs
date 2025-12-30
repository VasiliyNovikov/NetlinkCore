using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetlinkCore.Protocol;

internal unsafe ref struct SpanWriter(Span<byte> buffer)
{
    private readonly Span<byte> _buffer = buffer;
    private int _position = 0;

    public readonly Span<byte> Written => _buffer[.._position];

    public Span<byte> Skip(int length)
    {
        var slice = _buffer.Slice(_position, length);
        _position += Alignment.Align(length);
        return slice;
    }

    public ref T Skip<T>() where T : unmanaged => ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(Skip(sizeof(T))));

    public void Write(ReadOnlySpan<byte> data) => data.CopyTo(Skip(data.Length));

    public void Write<T>(in T value) where T : unmanaged => Skip<T>() = value;
}