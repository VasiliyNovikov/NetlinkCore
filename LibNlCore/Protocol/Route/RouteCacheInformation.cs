using System.Runtime.InteropServices;

namespace LibNlCore.Protocol.Route;

// struct rta_cacheinfo
[StructLayout(LayoutKind.Sequential)]
internal struct RouteCacheInformation
{
    public uint ClientReferences;
    public uint LastUse;
    public int Expires;
    public uint Error;
    public uint Used;
    public uint Id;
    public uint Timestamp;
    public uint TimestampAge;
}
