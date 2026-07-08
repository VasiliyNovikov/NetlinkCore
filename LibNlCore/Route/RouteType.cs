using System.Diagnostics.CodeAnalysis;

namespace LibNlCore.Route;

[SuppressMessage("Style", "IDE0055:Fix formatting")]
public enum RouteType
{
    Unspecified =  0, // RTN_UNSPEC
    Unicast     =  1, // RTN_UNICAST
    Local       =  2, // RTN_LOCAL
    Broadcast   =  3, // RTN_BROADCAST
    Anycast     =  4, // RTN_ANYCAST
    Multicast   =  5, // RTN_MULTICAST
    Blackhole   =  6, // RTN_BLACKHOLE
    Unreachable =  7, // RTN_UNREACHABLE
    Prohibited  =  8, // RTN_PROHIBIT
    Throw       =  9, // RTN_THROW
    Nat         = 10, // RTN_NAT
    XResolve    = 11  // RTN_XRESOLVE
}