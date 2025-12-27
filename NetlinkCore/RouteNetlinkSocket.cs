using NetlinkCore.Interop;

namespace NetlinkCore;

public sealed class RouteNetlinkSocket() : NetlinkSocket(LibC.NETLINK_ROUTE);