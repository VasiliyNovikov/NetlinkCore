namespace LibNlCore.Protocol.Route;

internal enum RouteMetricAttributes : ushort
{
    Unspecified,                // RTAX_UNSPEC
    Lock,                       // RTAX_LOCK
    Mtu,                        // RTAX_MTU
    Window,                     // RTAX_WINDOW
    RoundTripTime,              // RTAX_RTT
    RoundTripTimeVariance,      // RTAX_RTTVAR
    SlowStartThreshold,         // RTAX_SSTHRESH
    CongestionWindow,           // RTAX_CWND
    AdvertisedMss,              // RTAX_ADVMSS
    Reordering,                 // RTAX_REORDERING
    HopLimit,                   // RTAX_HOPLIMIT
    InitialCongestionWindow,    // RTAX_INITCWND
    Features,                   // RTAX_FEATURES
    MinimumRetransmissionTime,  // RTAX_RTO_MIN
    InitialReceiveWindow,       // RTAX_INITRWND
    QuickAck,                   // RTAX_QUICKACK
    CongestionControlAlgorithm, // RTAX_CC_ALGO
    FastOpenNoCookie            // RTAX_FASTOPEN_NO_COOKIE
}