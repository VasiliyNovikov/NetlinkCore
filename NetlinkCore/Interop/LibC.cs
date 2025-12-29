using System.Runtime.InteropServices;

namespace NetlinkCore.Interop;

internal static class LibC
{
    public const int NETLINK_ROUTE = 0;
    public const int NETLINK_NETFILTER = 12;

    public const int NETLINK_CAP_ACK = 10;
    public const int NETLINK_EXT_ACK = 11;
    public const int NETLINK_GET_STRICT_CHK = 12;

    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr_nl
    {
        public ushort nl_family; /* AF_NETLINK	*/
        public ushort nl_pad;    /* zero		*/
        public uint nl_pid;		 /* port ID	*/
        public uint nl_groups;	 /* multicast groups mask */
    }
}