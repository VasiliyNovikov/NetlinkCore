using System.Runtime.InteropServices;

namespace NetlinkCore.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct sockaddr_nl
{
    public ushort nl_family; /* AF_NETLINK	*/
    public ushort nl_pad;    /* zero		*/
    public uint nl_pid;		 /* port ID	*/
    public uint nl_groups;	 /* multicast groups mask */
}