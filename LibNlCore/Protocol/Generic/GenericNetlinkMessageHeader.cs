using System.Runtime.InteropServices;

namespace LibNlCore.Protocol.Generic;

// struct genlmsghdr
[StructLayout(LayoutKind.Sequential)]
internal struct GenericNetlinkMessageHeader
{
    public byte Cmd;      // cmd
    public byte Version;  // version
    private readonly ushort Reserved;
}