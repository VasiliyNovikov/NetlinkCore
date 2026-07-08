using System.Runtime.InteropServices;

using LinuxCore;

namespace LibNlCore.Protocol.Route;

// struct rtmsg
[StructLayout(LayoutKind.Sequential)]
internal struct RouteMessage
{
    private byte RawFamily;          // rtm_family
    public byte DestinationLength;   // rtm_dst_len
    public byte SourceLength;        // rtm_src_len
    public byte TypeOfService;       // rtm_tos
    public byte Table;               // rtm_table
    public byte Protocol;            // rtm_protocol
    public byte Scope;               // rtm_scope
    public byte RouteType;           // rtm_type
    public RouteMessageFlags Flags;  // rtm_flags

    public LinuxAddressFamily Family
    {
        readonly get => (LinuxAddressFamily)RawFamily;
        set => RawFamily = (byte)value;
    }
}
