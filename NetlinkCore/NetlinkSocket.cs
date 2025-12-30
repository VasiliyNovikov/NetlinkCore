using System.Net.Sockets;

using LinuxCore;

using NetlinkCore.Interop;

namespace NetlinkCore;

public abstract class NetlinkSocket : LinuxSocketBase
{
    public uint PortId { get; }

    protected NetlinkSocket(NetlinkFamily family) : base(LinuxAddressFamily.Netlink, LinuxSocketType.Raw, (ProtocolType)family)
    {
        SetOption(Constants.NETLINK_CAP_ACK, 1);
        SetOption(Constants.NETLINK_EXT_ACK, 1);
        SetOption(Constants.NETLINK_GET_STRICT_CHK, 1);
        var address = new sockaddr_nl
        {
            nl_family = (ushort)LinuxAddressFamily.Netlink,
            nl_pad = 0,
            nl_pid = 0,
            nl_groups = 0
        };
        Bind(address);
        Connect(address);
        GetAddress(out address);
        PortId = address.nl_pid;
    }

    private void SetOption(int option, int value) => base.SetOption(LinuxSocketOptionLevel.Netlink, option, value);
}