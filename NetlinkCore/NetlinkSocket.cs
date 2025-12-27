using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using NetlinkCore.Interop;

namespace NetlinkCore;

public abstract class NetlinkSocket : IDisposable
{
    private readonly int _fd;

    [SuppressMessage("Usage", "CA1816: Dispose methods should call SuppressFinalize")]
    protected unsafe NetlinkSocket(int protocol)
    {
        _fd = LibC.socket(LibC.AF_NETLINK, LibC.SOCK_RAW | LibC.SOCK_CLOEXEC, protocol);
        if (_fd == -1)
        {
            GC.SuppressFinalize(this);
            throw new Win32Exception();
        }

        var address = new LibC.sockaddr_nl
        {
            nl_family = LibC.AF_NETLINK,
            nl_pad = 0,
            nl_pid = 0,
            nl_groups = 0
        };
        if (LibC.bind(_fd, address, (uint)sizeof(LibC.sockaddr_nl)) == -1)
        {
            GC.SuppressFinalize(this);
            LibC.close(_fd);
            throw new Win32Exception();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        LibC.close(_fd);
    }

    ~NetlinkSocket() => LibC.close(_fd);
}