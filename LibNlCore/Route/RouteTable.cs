using System.Diagnostics.CodeAnalysis;

namespace LibNlCore.Route;

[SuppressMessage("Style", "IDE0055:Fix formatting")]
public static class RouteTable
{
    public const uint Unspecified =   0; // RT_TABLE_UNSPEC
    public const uint Compat      = 252; // RT_TABLE_COMPAT
    public const uint Default     = 253; // RT_TABLE_DEFAULT
    public const uint Main        = 254; // RT_TABLE_MAIN
    public const uint Local       = 255; // RT_TABLE_LOCAL
}