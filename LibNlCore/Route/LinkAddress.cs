using System;
using System.Net;

namespace LibNlCore.Route;

public sealed class LinkAddress : RouteAddress
{
    public bool NoDad { get; }

    public LinkAddress(IPAddress address, byte prefixLength, bool noDad = false)
        : base(address, prefixLength)
    {
        ArgumentOutOfRangeException.ThrowIfZero(prefixLength);
        NoDad = noDad;
    }

    public new static LinkAddress Parse(string addressString)
    {
        var (address, prefixLength) = ParseComponents(addressString);
        return new(address, prefixLength);
    }
}