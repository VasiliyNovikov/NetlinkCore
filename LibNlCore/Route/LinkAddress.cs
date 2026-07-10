using System.Net;

namespace LibNlCore.Route;

public sealed class LinkAddress(IPAddress address, byte prefixLength, bool noDad = false) : RouteAddress(address, prefixLength)
{
    public bool NoDad => noDad;

    public new static LinkAddress Parse(string addressString)
    {
        var (address, prefixLength) = ParseComponents(addressString);
        return new(address, prefixLength, noDad: true);
    }
}