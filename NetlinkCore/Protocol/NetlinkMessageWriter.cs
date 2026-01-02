using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetlinkCore.Interop;

namespace NetlinkCore.Protocol;

internal readonly ref struct NetlinkMessageWriter
{
    private readonly ref nlmsghdr _header;

    public int SubType
    {
        get => (int)((NetlinkMessageType)_header.nlmsg_type & ~NetlinkMessageType.Mask);
        set => _header.nlmsg_type = (ushort)((NetlinkMessageType)value | ((NetlinkMessageType)_header.nlmsg_type & NetlinkMessageType.Mask));
    }

    public NetlinkMessageFlags Flags
    {
        get => (NetlinkMessageFlags)_header.nlmsg_flags;
        set => _header.nlmsg_flags = (ushort)value;
    }

    public ref uint PortId => ref _header.nlmsg_pid;

    public SpanWriter PayloadWriter { get; }

    public ReadOnlySpan<byte> Written => PayloadWriter.Written;

    public NetlinkMessageWriter(Span<byte> buffer)
    {
        _header = ref Unsafe.As<byte, nlmsghdr>(ref MemoryMarshal.GetReference(buffer));
        _header = default;
        PayloadWriter = new SpanWriter(buffer, ref _header.nlmsg_len);
        PayloadWriter.Skip<nlmsghdr>();
    }
}