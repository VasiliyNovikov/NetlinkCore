namespace LibNlCore.Protocol.Route;

internal enum RouteAttributes : ushort
{
    Unspecified,       // RTA_UNSPEC
    Destination,       // RTA_DST
    Source,            // RTA_SRC
    InputInterface,    // RTA_IIF
    OutputInterface,   // RTA_OIF
    Gateway,           // RTA_GATEWAY
    Priority,          // RTA_PRIORITY
    PreferredSource,   // RTA_PREFSRC
    Metrics,           // RTA_METRICS
    Multipath,         // RTA_MULTIPATH
    ProtocolInfo,      // RTA_PROTOINFO
    Flow,              // RTA_FLOW
    CacheInfo,         // RTA_CACHEINFO
    Session,           // RTA_SESSION
    MultipathAlgorithm,// RTA_MP_ALGO
    Table,             // RTA_TABLE
    Mark,              // RTA_MARK
    MulticastStats,    // RTA_MFC_STATS
    Via,               // RTA_VIA
    NewDestination,    // RTA_NEWDST
    Preference,        // RTA_PREF
    EncapType,         // RTA_ENCAP_TYPE
    Encap,             // RTA_ENCAP
    Expires,           // RTA_EXPIRES
    Pad,               // RTA_PAD
    UserId,            // RTA_UID
    TtlPropagate,      // RTA_TTL_PROPAGATE
    IpProtocol,        // RTA_IP_PROTO
    SourcePort,        // RTA_SPORT
    DestinationPort,   // RTA_DPORT
    NextHopId          // RTA_NH_ID
}