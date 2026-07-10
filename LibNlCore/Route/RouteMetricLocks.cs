using System;

using LibNlCore.Protocol.Route;

namespace LibNlCore.Route;

[Flags]
public enum RouteMetricLocks : uint
{
    None                       = 0,
    Mtu                        = 1u << RouteMetricAttributes.Mtu,
    Window                     = 1u << RouteMetricAttributes.Window,
    RoundTripTime              = 1u << RouteMetricAttributes.RoundTripTime,
    RoundTripTimeVariance      = 1u << RouteMetricAttributes.RoundTripTimeVariance,
    SlowStartThreshold         = 1u << RouteMetricAttributes.SlowStartThreshold,
    CongestionWindow           = 1u << RouteMetricAttributes.CongestionWindow,
    AdvertisedMss              = 1u << RouteMetricAttributes.AdvertisedMss,
    Reordering                 = 1u << RouteMetricAttributes.Reordering,
    HopLimit                   = 1u << RouteMetricAttributes.HopLimit,
    InitialCongestionWindow    = 1u << RouteMetricAttributes.InitialCongestionWindow,
    Features                   = 1u << RouteMetricAttributes.Features,
    MinimumRetransmissionTime  = 1u << RouteMetricAttributes.MinimumRetransmissionTime,
    InitialReceiveWindow       = 1u << RouteMetricAttributes.InitialReceiveWindow,
    QuickAck                   = 1u << RouteMetricAttributes.QuickAck,
    CongestionControlAlgorithm = 1u << RouteMetricAttributes.CongestionControlAlgorithm,
    FastOpenNoCookie           = 1u << RouteMetricAttributes.FastOpenNoCookie
}