using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace LibNlCore.Route;

public class RouteAddress : IEquatable<RouteAddress>, IEqualityOperators<RouteAddress, RouteAddress, bool>
{
    public IPAddress Address { get; }
    public byte PrefixLength { get; }
    public AddressFamily AddressFamily => Address.AddressFamily;

    public RouteAddress(IPAddress address, byte prefixLength)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(prefixLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(prefixLength, address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);
        Address = address;
        PrefixLength = prefixLength;
    }

    public override string ToString() => $"{Address}/{PrefixLength}";

    protected static (IPAddress Address, byte PrefixLength) ParseComponents(string addressString)
    {
        var slashIndex = addressString.IndexOf('/');
        if (slashIndex < 0)
        {
            var address = IPAddress.Parse(addressString);
            var prefixLength = (byte)(address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);
            return (address, prefixLength);
        }
        else
        {
            var address = IPAddress.Parse(addressString.AsSpan(0, slashIndex));
            var prefixLength = byte.Parse(addressString.AsSpan(slashIndex + 1), CultureInfo.InvariantCulture);
            return (address, prefixLength);
        }
    }

    public static RouteAddress Parse(string addressString)
    {
        var (address, prefixLength) = ParseComponents(addressString);
        return new(address, prefixLength);
    }

    public bool Equals(RouteAddress? other) => other is not null && (ReferenceEquals(this, other) || Address.Equals(other.Address) && PrefixLength == other.PrefixLength);
    public override bool Equals(object? obj) => Equals(obj as RouteAddress);
    public override int GetHashCode() => HashCode.Combine(Address, PrefixLength);
    public static bool operator ==(RouteAddress? left, RouteAddress? right) => left is null && right is null || left is not null && left.Equals(right);
    public static bool operator !=(RouteAddress? left, RouteAddress? right) => !(left == right);
}