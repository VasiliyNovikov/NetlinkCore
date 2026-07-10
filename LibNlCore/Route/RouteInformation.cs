using System.Net;
using System.Net.Sockets;

namespace LibNlCore.Route;

public sealed class RouteInformation(AddressFamily addressFamily,
                                     IPAddress? source = null,
                                     byte sourcePrefixLength = 0,
                                     IPAddress? destination = null,
                                     byte destinationPrefixLength = 0,
                                     IPAddress? gateway = null,
                                     int? inputInterfaceIndex = null,
                                     int? outputInterfaceIndex = null,
                                     uint? priority = null,
                                     IPAddress? preferredSource = null,
                                     uint table = RouteTable.Main,
                                     RoutePreference? preference = null,
                                     RouteProtocol protocol = RouteProtocol.Static,
                                     RouteScope scope = RouteScope.Universe,
                                     RouteType type = RouteType.Unicast,
                                     byte typeOfService = 0)
{
    public AddressFamily AddressFamily => addressFamily;
    public IPAddress? Source => source;
    public byte SourcePrefixLength => sourcePrefixLength;
    public IPAddress? Destination => destination;
    public byte DestinationPrefixLength => destinationPrefixLength;
    public IPAddress? Gateway => gateway;
    public int? InputInterfaceIndex => inputInterfaceIndex;
    public int? OutputInterfaceIndex => outputInterfaceIndex;
    public uint? Priority => priority;
    public IPAddress? PreferredSource => preferredSource;
    public uint Table => table;
    public RoutePreference? Preference => preference;
    public RouteProtocol Protocol => protocol;
    public RouteScope Scope => scope;
    public RouteType Type => type;
    public byte TypeOfService => typeOfService;

    public RouteInformation(IPAddress destination,
                            byte destinationPrefixLength,
                            IPAddress? source = null,
                            byte sourcePrefixLength = 0,
                            IPAddress? gateway = null,
                            int? inputInterfaceIndex = null,
                            int? outputInterfaceIndex = null,
                            uint? priority = null,
                            IPAddress? preferredSource = null,
                            uint table = RouteTable.Main,
                            RoutePreference? preference = null,
                            RouteProtocol protocol = RouteProtocol.Static,
                            RouteScope scope = RouteScope.Universe,
                            RouteType type = RouteType.Unicast,
                            byte typeOfService = 0)
        : this(destination.AddressFamily, source, sourcePrefixLength, destination, destinationPrefixLength, gateway, inputInterfaceIndex, outputInterfaceIndex, priority, preferredSource, table, preference, protocol, scope, type, typeOfService)
    {
    }

    public RouteInformation WithOutputInterfaceIndex(int? outputInterfaceIndex)
    {
        return new(AddressFamily, Source, SourcePrefixLength, Destination, DestinationPrefixLength, Gateway, InputInterfaceIndex, outputInterfaceIndex, Priority, PreferredSource, Table, Preference, Protocol, Scope, Type, TypeOfService);
    }
}