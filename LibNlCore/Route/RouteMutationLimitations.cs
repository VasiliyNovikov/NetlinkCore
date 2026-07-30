using System;

namespace LibNlCore.Route;

[Flags]
internal enum RouteMutationLimitations
{
    None = 0,
    NextHopId = 1 << 0,
    Multipath = 1 << 1,
    Encapsulation = 1 << 2,
    Attributes = 1 << 3,
    Metrics = 1 << 4,
    Flags = 1 << 5
}