using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using LinuxCore;

namespace LibNlCore;

public static class TcpCongestionControlAlgorithms
{
    private const string AvailableAlgorithmsPath = "/proc/sys/net/ipv4/tcp_available_congestion_control";
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(10);
    private static readonly Lock Lock = new();
    private static FrozenSet<string>? _available;
    private static TimeSpan _refreshTimestamp;

    public static IReadOnlySet<string> Available
    {
        get
        {
            lock (Lock)
                return GetAvailableUnsafe(out _);
        }
    }

    public static bool IsAvailable(string algorithm)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        lock (Lock)
            // Refresh a cache miss so newly registered algorithms do not remain unavailable for the rest of the heuristic cache lifetime
            return GetAvailableUnsafe(out var refreshed).Contains(algorithm) || !refreshed && RefreshUnsafe().Contains(algorithm);
    }

    private static FrozenSet<string> GetAvailableUnsafe(out bool refreshed)
    {
        var available = _available;
        if (available is null || LinuxClock.Monotonic - _refreshTimestamp > CacheLifetime)
        {
            available = RefreshUnsafe();
            refreshed = true;
        }
        else
            refreshed = false;
        return available;
    }

    private static FrozenSet<string> RefreshUnsafe()
    {
        if (File.ReadAllText(AvailableAlgorithmsPath)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .ToFrozenSet(StringComparer.Ordinal) is not { Count: not 0 } available)
            throw new InvalidDataException("Linux did not report any available TCP congestion control algorithms");
        _available = available;
        _refreshTimestamp = LinuxClock.Monotonic;
        return available;
    }
}