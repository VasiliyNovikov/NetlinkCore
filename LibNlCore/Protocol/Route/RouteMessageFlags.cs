using System;
using System.Diagnostics.CodeAnalysis;

namespace LibNlCore.Protocol.Route;

[Flags]
[SuppressMessage("Style", "IDE0055:Fix formatting")]
internal enum RouteMessageFlags : uint
{
    None          =          0,
    Notify        =      0x100, // RTM_F_NOTIFY
    Cloned        =      0x200, // RTM_F_CLONED
    Equalize      =      0x400, // RTM_F_EQUALIZE
    Prefix        =      0x800, // RTM_F_PREFIX
    LookupTable   =     0x1000, // RTM_F_LOOKUP_TABLE
    FibMatch      =     0x2000, // RTM_F_FIB_MATCH
    Offloaded     =     0x4000, // RTM_F_OFFLOAD
    Trap          =     0x8000, // RTM_F_TRAP
    OffloadFailed = 0x20000000  // RTM_F_OFFLOAD_FAILED
}