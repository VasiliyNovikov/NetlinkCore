using System;

namespace NetlinkCore.Protocol;

internal readonly ref struct NetlinkMessage(NetlinkMessageType type, int subType, NetlinkMessageFlags flags, ReadOnlySpan<byte> payload)
{
    public NetlinkMessageType Type => type;
    public int SubType => subType;
    public NetlinkMessageFlags Flags => flags;
    public ReadOnlySpan<byte> Payload { get; } = payload;
}