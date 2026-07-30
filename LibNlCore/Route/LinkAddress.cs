using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace LibNlCore.Route;

public sealed class LinkAddress : IEquatable<LinkAddress>
{
    public IPAddress Address { get; }
    public byte PrefixLength { get; }
    public bool NoDad { get; }
    public AddressFamily AddressFamily => Address.AddressFamily;

    public LinkAddress(IPAddress address, byte prefixLength, bool noDad = false)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentOutOfRangeException.ThrowIfZero(prefixLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(prefixLength, address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);
        Address = address;
        PrefixLength = prefixLength;
        NoDad = noDad;
    }

    public static LinkAddress Parse(string addressString)
    {
        var slashIndex = addressString.IndexOf('/');
        if (slashIndex < 0)
        {
            var parsedAddress = IPAddress.Parse(addressString);
            var parsedPrefixLength = (byte)(parsedAddress.AddressFamily == AddressFamily.InterNetwork ? 32 : 128);
            return new(parsedAddress, parsedPrefixLength);
        }

        var address = IPAddress.Parse(addressString.AsSpan(0, slashIndex));
        var prefixLength = byte.Parse(addressString.AsSpan(slashIndex + 1), CultureInfo.InvariantCulture);
        return new(address, prefixLength);
    }

    public override string ToString() => $"{Address}/{PrefixLength}";
    public bool Equals(LinkAddress? other) => other is not null && (ReferenceEquals(this, other) || Address.Equals(other.Address) && PrefixLength == other.PrefixLength && NoDad == other.NoDad);
    public override bool Equals(object? obj) => Equals(obj as LinkAddress);
    public override int GetHashCode() => HashCode.Combine(Address, PrefixLength, NoDad);
    public static bool operator ==(LinkAddress? left, LinkAddress? right) => left is null && right is null || left is not null && left.Equals(right);
    public static bool operator !=(LinkAddress? left, LinkAddress? right) => !(left == right);
}