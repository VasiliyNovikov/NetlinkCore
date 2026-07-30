using System;

namespace LibNlCore.Route;

public sealed class RouteMetrics(RouteMetricLocks locks = RouteMetricLocks.None,
                                 uint? mtu = null,
                                 uint? window = null,
                                 TimeSpan? roundTripTime = null,
                                 TimeSpan? roundTripTimeVariance = null,
                                 uint? slowStartThreshold = null,
                                 uint? congestionWindow = null,
                                 uint? advertisedMss = null,
                                 uint? reordering = null,
                                 uint? hopLimit = null,
                                 uint? initialCongestionWindow = null,
                                 RouteMetricFeatures features = RouteMetricFeatures.None,
                                 TimeSpan? minimumRetransmissionTime = null,
                                 uint? initialReceiveWindow = null,
                                 uint? quickAck = null,
                                 string? congestionControlAlgorithm = null,
                                 uint? fastOpenNoCookie = null)
{
    public RouteMetricLocks Locks => locks;
    public uint? Mtu => mtu;
    public uint? Window => window;
    public TimeSpan? RoundTripTime => roundTripTime;
    public TimeSpan? RoundTripTimeVariance => roundTripTimeVariance;
    public uint? SlowStartThreshold => slowStartThreshold;
    public uint? CongestionWindow => congestionWindow;
    public uint? AdvertisedMss => advertisedMss;
    public uint? Reordering => reordering;
    public uint? HopLimit => hopLimit;
    public uint? InitialCongestionWindow => initialCongestionWindow;
    public RouteMetricFeatures Features => features;
    public TimeSpan? MinimumRetransmissionTime => minimumRetransmissionTime;
    public uint? InitialReceiveWindow => initialReceiveWindow;
    public uint? QuickAck => quickAck;
    public string? CongestionControlAlgorithm => congestionControlAlgorithm;
    public uint? FastOpenNoCookie => fastOpenNoCookie;

    internal bool IsEmpty => Locks == RouteMetricLocks.None &&
                             Mtu is null &&
                             Window is null &&
                             RoundTripTime is null &&
                             RoundTripTimeVariance is null &&
                             SlowStartThreshold is null &&
                             CongestionWindow is null &&
                             AdvertisedMss is null &&
                             Reordering is null &&
                             HopLimit is null &&
                             InitialCongestionWindow is null &&
                             Features == RouteMetricFeatures.None &&
                             MinimumRetransmissionTime is null &&
                             InitialReceiveWindow is null &&
                             QuickAck is null &&
                             CongestionControlAlgorithm is null &&
                             FastOpenNoCookie is null;
}