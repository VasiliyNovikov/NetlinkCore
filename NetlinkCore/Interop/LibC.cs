using System.Runtime.InteropServices;

namespace NetlinkCore.Interop;

internal static partial class LibC
{
    private const string Lib = "libc";

    public const int AF_NETLINK = 16;
    
    public const int SOCK_RAW = 3;
    public const int SOCK_CLOEXEC = 0x80000;
    
    public const int NETLINK_ROUTE = 0;
    public const int NETLINK_NETFILTER = 12;
    
    public const int NETLINK_CAP_ACK = 10;
    public const int NETLINK_EXT_ACK = 11;
    public const int NETLINK_GET_STRICT_CHK = 12;

    // int close(int fd);
    [LibraryImport(Lib, EntryPoint = "close", SetLastError = true)]
    public static partial int close(int fd);

    // int socket(int domain, int type, int protocol);
    [LibraryImport(Lib, EntryPoint = "socket", SetLastError = true)]
    public static partial int socket(int domain, int type, int protocol);

    // int bind(int sockfd, const struct sockaddr_nl *addr, socklen_t addrlen);
    [LibraryImport(Lib, EntryPoint = "bind", SetLastError = true)]
    public static partial int bind(int sockfd, in sockaddr_nl addr, uint addrlen);

    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr_nl
    {
        public ushort nl_family; /* AF_NETLINK	*/
        public ushort nl_pad; /* zero		*/
        public uint nl_pid;		/* port ID	*/
        public uint nl_groups;	/* multicast groups mask */
    };
}