using System.Runtime.InteropServices;

namespace NetlinkCore.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct nlmsghdr
{
    public uint nlmsg_len;     /* Length of message including headers */
    public ushort nlmsg_type;  /* Netlink Family (subsystem) ID */
    public ushort nlmsg_flags; /* Flags - request or dump */
    public uint nlmsg_seq;     /* Sequence number */
    public uint nlmsg_pid;     /* Port ID, set to 0 */
}