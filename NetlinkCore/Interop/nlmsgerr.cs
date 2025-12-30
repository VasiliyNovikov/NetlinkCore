using System.Runtime.InteropServices;

namespace NetlinkCore.Interop;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct nlmsgerr
{
    public readonly int error;
    public readonly nlmsghdr msg;
    /*
     * followed by the message contents unless NETLINK_CAP_ACK was set
     * or the ACK indicates success (error == 0)
     * message length is aligned with NLMSG_ALIGN()
     */
    /*
     * followed by TLVs defined in enum nlmsgerr_attrs
     * if NETLINK_EXT_ACK was set
     */
}