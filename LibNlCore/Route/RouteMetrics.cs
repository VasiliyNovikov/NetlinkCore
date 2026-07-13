using System;
using System.Runtime.CompilerServices;

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
    internal const long RoundTripTimeTicksPerUnit = TimeSpan.TicksPerMillisecond / 8;
    internal const long RoundTripTimeVarianceTicksPerUnit = TimeSpan.TicksPerMillisecond / 4;
    internal const long MinimumRetransmissionTimeTicksPerUnit = TimeSpan.TicksPerMillisecond;

    public RouteMetricLocks Locks => locks;
    public uint? Mtu => mtu;
    public uint? Window => window;
    public TimeSpan? RoundTripTime { get; } = ValidateTimeSpan(roundTripTime, RoundTripTimeTicksPerUnit);
    public TimeSpan? RoundTripTimeVariance { get; } = ValidateTimeSpan(roundTripTimeVariance, RoundTripTimeVarianceTicksPerUnit);
    public uint? SlowStartThreshold => slowStartThreshold;
    public uint? CongestionWindow => congestionWindow;
    public uint? AdvertisedMss => advertisedMss;
    public uint? Reordering => reordering;
    public uint? HopLimit => hopLimit;
    public uint? InitialCongestionWindow => initialCongestionWindow;
    public RouteMetricFeatures Features => features;
    public TimeSpan? MinimumRetransmissionTime { get; } = ValidateTimeSpan(minimumRetransmissionTime, MinimumRetransmissionTimeTicksPerUnit);
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

    internal static TimeSpan DecodeTimeSpan(uint value, long ticksPerUnit) => TimeSpan.FromTicks(value * ticksPerUnit);
    internal static uint EncodeTimeSpan(TimeSpan value, long ticksPerUnit) => checked((uint)(value.Ticks / ticksPerUnit));

    private static TimeSpan? ValidateTimeSpan(TimeSpan? value, long ticksPerUnit, [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        return value is { } actual
            ? actual >= TimeSpan.Zero && actual.Ticks / ticksPerUnit <= uint.MaxValue
                ? actual
                : throw new ArgumentOutOfRangeException(paramName, value, "The interval must be non-negative and fit in a 32-bit route metric.")
            : null;
    }
}