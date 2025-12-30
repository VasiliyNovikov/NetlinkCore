namespace NetlinkCore.Interop.Route;

internal enum ifinfomsg_attrs
{
    IFLA_UNSPEC,
    IFLA_ADDRESS,
    IFLA_BROADCAST,
    IFLA_IFNAME,
    IFLA_MTU,
    IFLA_LINK,
    IFLA_QDISC,
    IFLA_STATS,
    IFLA_COST,
    IFLA_PRIORITY,
    IFLA_MASTER,
    IFLA_WIRELESS,		/* Wireless Extension event - see wireless.h */
    IFLA_PROTINFO,		/* Protocol specific information for a link */
    IFLA_TXQLEN,
    IFLA_MAP,
    IFLA_WEIGHT,
    IFLA_OPERSTATE,
    IFLA_LINKMODE,
    IFLA_LINKINFO,
    IFLA_NET_NS_PID,
    IFLA_IFALIAS,
    IFLA_NUM_VF,		/* Number of VFs if device is SR-IOV PF */
    IFLA_VFINFO_LIST,
    IFLA_STATS64,
    IFLA_VF_PORTS,
    IFLA_PORT_SELF,
    IFLA_AF_SPEC,
    IFLA_GROUP,		/* Group the device belongs to */
    IFLA_NET_NS_FD,
    IFLA_EXT_MASK,		/* Extended info mask, VFs, etc */
    IFLA_PROMISCUITY,	/* Promiscuity count: > 0 means acts PROMISC */
    IFLA_NUM_TX_QUEUES,
    IFLA_NUM_RX_QUEUES,
    IFLA_CARRIER,
    IFLA_PHYS_PORT_ID,
    IFLA_CARRIER_CHANGES,
    IFLA_PHYS_SWITCH_ID,
    IFLA_LINK_NETNSID,
    IFLA_PHYS_PORT_NAME,
    IFLA_PROTO_DOWN,
    IFLA_GSO_MAX_SEGS,
    IFLA_GSO_MAX_SIZE,
    IFLA_PAD,
    IFLA_XDP,
    IFLA_EVENT,
    IFLA_NEW_NETNSID,
    IFLA_IF_NETNSID,
    IFLA_TARGET_NETNSID = IFLA_IF_NETNSID, /* new alias */
    IFLA_CARRIER_UP_COUNT,
    IFLA_CARRIER_DOWN_COUNT,
    IFLA_NEW_IFINDEX,
    IFLA_MIN_MTU,
    IFLA_MAX_MTU,
    IFLA_PROP_LIST,
    IFLA_ALT_IFNAME, /* Alternative ifname */
    IFLA_PERM_ADDRESS,
    IFLA_PROTO_DOWN_REASON,

    /* device (sysfs) name as parent, used instead
     * of IFLA_LINK where there's no parent netdev
     */
    IFLA_PARENT_DEV_NAME,
    IFLA_PARENT_DEV_BUS_NAME,
}