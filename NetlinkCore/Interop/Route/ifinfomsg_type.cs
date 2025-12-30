namespace NetlinkCore.Interop.Route;

internal enum ifinfomsg_type
{
    RTM_NEWLINK	= 16,
    RTM_DELLINK,
    RTM_GETLINK,
    RTM_SETLINK,

    RTM_NEWADDR	= 20,
    RTM_DELADDR,
    RTM_GETADDR,
}