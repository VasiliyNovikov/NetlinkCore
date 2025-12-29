using System.Net.Sockets;

using LinuxCore;

using NetlinkCore.Interop;

namespace NetlinkCore;

public abstract class NetlinkSocket : LinuxSocketBase
{
    protected NetlinkSocket(int protocol) : base(LinuxAddressFamily.Netlink, LinuxSocketType.Raw, (ProtocolType)protocol)
    {
        SetOption(LibC.NETLINK_CAP_ACK, 1);
        SetOption(LibC.NETLINK_EXT_ACK, 1);
        SetOption(LibC.NETLINK_GET_STRICT_CHK, 1);

        var address = new LibC.sockaddr_nl
        {
            nl_family = (ushort)LinuxAddressFamily.Netlink,
            nl_pad = 0,
            nl_pid = 0,
            nl_groups = 0
        };
        Bind(address);
    }

    private void SetOption(int option, int value) => base.SetOption(LinuxSocketOptionLevel.Netlink, option, value);
}