using System;

namespace LibNlCore.Route;

[Flags]
public enum RouteMetricFeatures : uint
{
    None                     = 0,
    Ecn                      = 1u << 0, // RTAX_FEATURE_ECN
    Sack                     = 1u << 1, // RTAX_FEATURE_SACK
    Timestamp                = 1u << 2, // RTAX_FEATURE_TIMESTAMP
    AllFragments             = 1u << 3, // RTAX_FEATURE_ALLFRAG
    TcpMicrosecondTimestamps = 1u << 4  // RTAX_FEATURE_TCP_USEC_TS
}