using NetlinkCore.Interop;

namespace NetlinkCore.Protocol;

public enum NetlinkMessageType
{
    None,
    NoOp = Constants.NLMSG_NOOP,
    Error = Constants.NLMSG_ERROR,
    Done = Constants.NLMSG_DONE,
    Overrun = Constants.NLMSG_OVERRUN,

    Mask = 0xf
}