namespace LibNlCore.Route;

public enum RoutePreference : byte
{
    Medium   = 0x0, // ICMPV6_ROUTER_PREF_MEDIUM
    High     = 0x1, // ICMPV6_ROUTER_PREF_HIGH
    Reserved = 0x2, // ICMPV6_ROUTER_PREF_INVALID
    Low      = 0x3, // ICMPV6_ROUTER_PREF_LOW
}