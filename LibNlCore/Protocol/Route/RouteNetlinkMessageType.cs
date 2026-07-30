using System.Diagnostics.CodeAnalysis;

namespace LibNlCore.Protocol.Route;

[SuppressMessage("Style", "IDE0055:Fix formatting")]
public enum RouteNetlinkMessageType : ushort
{
    NewLink       = 16, // RTM_NEWLINK
    DeleteLink    = 17, // RTM_DELLINK
    GetLink       = 18, // RTM_GETLINK
    SetLink       = 19, // RTM_SETLINK

    NewAddress    = 20, // RTM_NEWADDR
    DeleteAddress = 21, // RTM_DELADDR
    GetAddress    = 22, // RTM_GETADDR

    NewRoute      = 24, // RTM_NEWROUTE
    DeleteRoute   = 25, // RTM_DELROUTE
    GetRoute      = 26, // RTM_GETROUTE
}