using System.Net;
using System.Net.Sockets;

namespace LibNlCore.Route;

public sealed class RouteInformation(AddressFamily addressFamily,
                                     IPAddress? destination = null,
                                     byte destinationPrefixLength = 0,
                                     IPAddress? gateway = null,
                                     int? inputInterfaceIndex = null,
                                     int? outputInterfaceIndex = null,
                                     uint? priority = null,
                                     IPAddress? preferredSource = null,
                                     uint table = RouteTable.Main,
                                     RouteProtocol protocol = RouteProtocol.Static,
                                     RouteScope scope = RouteScope.Universe,
                                     RouteType type = RouteType.Unicast)
{
    public AddressFamily AddressFamily => addressFamily;
    public IPAddress? Destination => destination;
    public byte DestinationPrefixLength => destinationPrefixLength;
    public IPAddress? Gateway => gateway;
    public int? InputInterfaceIndex => inputInterfaceIndex;
    public int? OutputInterfaceIndex => outputInterfaceIndex;
    public uint? Priority => priority;
    public IPAddress? PreferredSource => preferredSource;
    public uint Table => table;
    public RouteProtocol Protocol => protocol;
    public RouteScope Scope => scope;
    public RouteType Type => type;

    public RouteInformation(IPAddress destination,
                            byte destinationPrefixLength,
                            IPAddress? gateway = null,
                            int? inputInterfaceIndex = null,
                            int? outputInterfaceIndex = null,
                            uint? priority = null,
                            IPAddress? preferredSource = null,
                            uint table = RouteTable.Main,
                            RouteProtocol protocol = RouteProtocol.Static,
                            RouteScope scope = RouteScope.Universe,
                            RouteType type = RouteType.Unicast)
        : this(destination.AddressFamily, destination, destinationPrefixLength, gateway, inputInterfaceIndex, outputInterfaceIndex, priority, preferredSource, table, protocol, scope, type)
    {
    }

    public RouteInformation WithOutputInterfaceIndex(int? outputInterfaceIndex)
    {
        return new RouteInformation(AddressFamily, Destination, DestinationPrefixLength, Gateway, InputInterfaceIndex, outputInterfaceIndex, Priority, PreferredSource, Table, Protocol, Scope, Type);
    }
}