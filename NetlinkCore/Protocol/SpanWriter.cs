using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#pragma warning disable CS9084 // Struct member returns 'this' or other instance members by reference

namespace NetlinkCore.Protocol;

internal readonly unsafe ref struct SpanWriter
{
    private readonly Span<byte> _buffer;
    private readonly ref uint _length;

    public ref readonly uint Length => ref _length;

    public ReadOnlySpan<byte> Written => _buffer[..(int)_length];

    public SpanWriter(Span<byte> buffer, ref uint length)
    {
        _buffer = buffer;
        _length = ref length;
    }

    public Span<byte> Skip(int length)
    {
        var slice = _buffer.Slice((int)_length, length);
        _length += Alignment.Align((uint)length);
        return slice;
    }

    public ref T Skip<T>() where T : unmanaged => ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(Skip(sizeof(T))));

    public void Write(ReadOnlySpan<byte> data) => data.CopyTo(Skip(data.Length));

    public void Write<T>(in T value) where T : unmanaged => Skip<T>() = value;
}