using System.Net;
using System.Net.Sockets;

using NetworkingPrimitivesCore;

namespace LibNlCore.Route;

public sealed class RouteInformation(AddressFamily addressFamily,
                                     IPAnyNetwork? source = null,
                                     IPAnyNetwork? destination = null,
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
                                     byte typeOfService = 0,
                                     RouteMetrics? metrics = null)
{
    public AddressFamily AddressFamily => addressFamily;
    public IPAnyNetwork? Source => source;
    public IPAnyNetwork? Destination => destination;
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
    public RouteMetrics? Metrics => metrics;

    public RouteInformation(IPAnyNetwork destination,
                            IPAnyNetwork? source = null,
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
                            byte typeOfService = 0,
                            RouteMetrics? metrics = null)
        : this(destination.Address.AddressFamily, source, destination, gateway, inputInterfaceIndex, outputInterfaceIndex, priority, preferredSource, table, preference, protocol, scope, type, typeOfService, metrics)
    {
    }

    public RouteInformation WithOutputInterfaceIndex(int outputInterfaceIndex)
    {
        return new(AddressFamily, Source, Destination, Gateway, InputInterfaceIndex, outputInterfaceIndex, Priority, PreferredSource, Table, Preference, Protocol, Scope, Type, TypeOfService, Metrics);
    }
}