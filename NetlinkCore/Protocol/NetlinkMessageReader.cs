using System;

using NetlinkCore.Interop;

namespace NetlinkCore.Protocol;

internal readonly unsafe ref struct NetlinkMessageReader
{
    public NetlinkMessageFlags Flags { get; }
    public ReadOnlySpan<byte> Payload { get; }

    public NetlinkMessageReader(SpanReader reader)
    {
        ref readonly var header = ref reader.Read<nlmsghdr>();
        Flags = (NetlinkMessageFlags)header.nlmsg_flags;
        Payload = reader.Read((int)header.nlmsg_len - sizeof(nlmsghdr));
    }
}