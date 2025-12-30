namespace NetlinkCore.Interop;

internal static class Constants
{
    public const int NETLINK_ROUTE = 0;

    public const int NETLINK_CAP_ACK = 10;
    public const int NETLINK_EXT_ACK = 11;
    public const int NETLINK_GET_STRICT_CHK = 12;
    
    public const int NLM_F_REQUEST       = 0x01; /* It is request message */
    public const int NLM_F_MULTI         = 0x02; /* Multipart message, terminated by NLMSG_DONE */
    public const int NLM_F_ACK           = 0x04; /* Reply with ack, with zero or error code */
    public const int NLM_F_ECHO          = 0x08; /* Echo this request */
    public const int NLM_F_DUMP_INTR     = 0x10; /* Dump was inconsistent due to sequence change */
    public const int NLM_F_DUMP_FILTERED = 0x20; /* Dump was filtered as requested */
    /* Modifiers to GET request */
    public const int NLM_F_ROOT          = 0x100; /* Specify tree root */
    public const int NLM_F_MATCH         = 0x200; /* Return all matching */
    public const int NLM_F_ATOMIC        = 0x400; /* Atomic GET */
    public const int NLM_F_DUMP          = NLM_F_ROOT | NLM_F_MATCH;
    /* Modifiers to NEW request */
    public const int NLM_F_REPLACE       = 0x100; /* Override existing */
    public const int NLM_F_EXCL          = 0x200; /* Do not touch, if it exists */
    public const int NLM_F_CREATE        = 0x400; /* Create, if it does not exist */
    public const int NLM_F_APPEND        = 0x800; /* Add to end of list */
    /* Modifiers to DELETE request */
    public const int NLM_F_NONREC        = 0x100; /* Do not delete recursively */
    /* Flags for ACK message */
    public const int NLM_F_CAPPED        = 0x100; /* capped request */
    public const int NLM_F_ACK_TLVS      = 0x200; /* extended ACK TLVs */

    public const int NLMSG_NOOP    = 0x1; /* Nothing */
    public const int NLMSG_ERROR   = 0x2; /* Error */
    public const int NLMSG_DONE    = 0x3; /* End of a dump */
    public const int NLMSG_OVERRUN = 0x4; /* Data lost */

    public const int NLMSGERR_ATTR_MSG    = 0x1; // error message string (string)
    public const int NLMSGERR_ATTR_OFFS   = 0x2; // offset of error (u32)
    public const int NLMSGERR_ATTR_COOKIE = 0x3; // cookie to associate with request (u32)
    public const int NLMSGERR_ATTR_POLICY = 0x4; // nlmsgerr policy (nested)
}