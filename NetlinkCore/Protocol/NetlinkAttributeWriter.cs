using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using NetlinkCore.Interop.Route;

namespace NetlinkCore.Protocol;

internal readonly unsafe ref struct NetlinkAttributeWriter<TAttr>(SpanWriter writer)
    where TAttr : unmanaged, Enum
{
    private readonly SpanWriter _writer = writer;

    public Span<byte> Write(TAttr name, int length)
    {
        ref var header = ref _writer.Skip<rtattr>();
        header.rta_type = Unsafe.BitCast<TAttr, ushort>(name);
        header.rta_len = (ushort)(length + Unsafe.SizeOf<rtattr>());
        return _writer.Skip(length);
    }

    public void Write<T>(TAttr name, in T value) where T : unmanaged => MemoryMarshal.Write(Write(name, sizeof(T)), value);

    public void Write(TAttr name, ReadOnlySpan<byte> value) => value.CopyTo(Write(name, value.Length));

    public void Write(TAttr name, string value)
    {
        var buffer = Write(name, Encoding.UTF8.GetByteCount(value) + 1);
        Encoding.UTF8.GetBytes(value, buffer);
        buffer[^1] = 0;
    }

    public NestedScope WriteNested(TAttr name)
    {
        ref var header = ref _writer.Skip<rtattr>();
        header.rta_type = Unsafe.BitCast<TAttr, ushort>(name);
        return new NestedScope(_writer, ref header.rta_len);
    }

    public readonly ref struct NestedScope : IDisposable
    {
        private readonly uint _writerStart;
        private readonly ref readonly uint _writerLength;
        private readonly ref ushort _length;

        internal NestedScope(SpanWriter writer, ref ushort length)
        {
            _writerStart = writer.Length;
            _writerLength = ref writer.Length;
            _length = ref length;
        }

        public void Dispose() => _length = (ushort)(_writerLength - _writerStart + sizeof(rtattr));
    }
}