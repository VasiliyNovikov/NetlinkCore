using System.Diagnostics.CodeAnalysis;

namespace LibNlCore.Route;

[SuppressMessage("Style", "IDE0055:Fix formatting")]
public enum RouteScope : byte
{
    Universe =   0, // RT_SCOPE_UNIVERSE
    Site     = 200, // RT_SCOPE_SITE
    Link     = 253, // RT_SCOPE_LINK
    Host     = 254, // RT_SCOPE_HOST
    NoWhere  = 255  // RT_SCOPE_NOWHERE
}