using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using NetlinkCore.Interop;

namespace NetlinkCore;

public abstract unsafe class NetlinkSocket : IDisposable
{
    private readonly int _fd;

    [SuppressMessage("Usage", "CA1816: Dispose methods should call SuppressFinalize")]
    protected NetlinkSocket(int protocol)
    {
        _fd = LibC.socket(LibC.AF_NETLINK, LibC.SOCK_RAW | LibC.SOCK_CLOEXEC, protocol);
        try
        {
            if (_fd == -1)
                throw new Win32Exception();

            SetOption(LibC.NETLINK_CAP_ACK, 1);
            SetOption(LibC.NETLINK_EXT_ACK, 1);
            SetOption(LibC.NETLINK_GET_STRICT_CHK, 1);

            var address = new LibC.sockaddr_nl
            {
                nl_family = LibC.AF_NETLINK,
                nl_pad = 0,
                nl_pid = 0,
                nl_groups = 0
            };
            if (LibC.bind(_fd, address, (uint)sizeof(LibC.sockaddr_nl)) == -1)
                throw new Win32Exception();
        }
        catch
        {
            GC.SuppressFinalize(this);
            if (_fd != -1)
                LibC.close(_fd);
            throw;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        LibC.close(_fd);
    }

    ~NetlinkSocket() => LibC.close(_fd);

    private void SetOption(int option, int value)
    {
        if (LibC.setsockopt(_fd, LibC.SOL_NETLINK, option, &value, sizeof(int)) == -1)
            throw new Win32Exception();
    }
}