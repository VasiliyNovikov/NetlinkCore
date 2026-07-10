using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using LibNlCore.Protocol;
using LibNlCore.Protocol.Route;

using LinuxCore;

using NetNsCore;

using NetworkingPrimitivesCore;

namespace LibNlCore.Route;

public sealed class RouteNetlinkSocket() : NetlinkSocket(NetlinkFamily.Route)
{
    #region Links

    public LinkInformation GetLink(string name)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteLinkMessage, RouteLinkAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.GetLink;
        writer.Flags = NetlinkMessageFlags.Request;
        writer.Attributes.Write(RouteLinkAttributes.Name, name);
        foreach (var message in Get(buffer, writer))
            if (message.Type == RouteNetlinkMessageType.NewLink)
                return ParseLink(message);
        throw new InvalidOperationException($"Link with name '{name}' not found");
    }

    public LinkInformation GetLink(int index)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteLinkMessage, RouteLinkAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.GetLink;
        writer.Flags = NetlinkMessageFlags.Request;
        writer.Header.Index = index;
        foreach (var message in Get(buffer, writer))
            if (message.Type == RouteNetlinkMessageType.NewLink)
                return ParseLink(message);
        throw new InvalidOperationException($"Link with index '{index}' not found");
    }

    public LinkInformation[] GetLinks()
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Large);
        var writer = GetWriter<RouteLinkMessage, RouteLinkAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.GetLink;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Dump;
        var links = new List<LinkInformation>();
        foreach (var message in Get(buffer, writer))
            if (message.Type == RouteNetlinkMessageType.NewLink)
                links.Add(ParseLink(message));
        return [.. links];
    }

    public void UpdateLink(LinkInformation origLinkInformation, LinkInformation linkInformation)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteLinkMessage, RouteLinkAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.SetLink;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Ack;
        writer.Header.Index = origLinkInformation.Index;
        if (origLinkInformation.Up != linkInformation.Up)
        {
            writer.Header.Flags = linkInformation.Up ? NetDeviceFlags.Up : 0;
            writer.Header.Change = NetDeviceFlags.Up;
        }
        if (origLinkInformation.Name != linkInformation.Name)
            writer.Attributes.Write(RouteLinkAttributes.Name, linkInformation.Name);
        if (origLinkInformation.MacAddress != linkInformation.MacAddress && linkInformation.MacAddress is { } macAddress)
            writer.Attributes.Write(RouteLinkAttributes.Address, macAddress);
        if (origLinkInformation.MasterIndex != linkInformation.MasterIndex)
            writer.Attributes.Write(RouteLinkAttributes.Master, linkInformation.MasterIndex ?? 0);
        Post(buffer, writer);
    }

    public void DeleteLink(string name)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteLinkMessage, RouteLinkAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.DeleteLink;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Ack;
        writer.Attributes.Write(RouteLinkAttributes.Name, name);
        Post(buffer, writer);
    }

    public void DeleteLink(int index)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteLinkMessage, RouteLinkAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.DeleteLink;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Ack;
        writer.Header.Index = index;
        Post(buffer, writer);
    }

    public void CreateVEth(string name, string peerName, int? rxQueueCount = null, int? txQueueCount = null)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = BeginCreateLink(buffer, name, rxQueueCount, txQueueCount);
        using (var infoAttrs = writer.Attributes.WriteNested<RouteLinkInfoAttributes>(RouteLinkAttributes.LinkInfo))
        {
            infoAttrs.Writer.Write(RouteLinkInfoAttributes.Kind, "veth");
            using var vethAttrs = infoAttrs.Writer.WriteNested<VethInfoAttributes>(RouteLinkInfoAttributes.Data);
            using var peerAttrs = vethAttrs.Writer.WriteNested<RouteLinkAttributes, RouteLinkMessage>(VethInfoAttributes.Peer);
            peerAttrs.Header = default;
            peerAttrs.Writer.Write(RouteLinkAttributes.Name, peerName);
            if (rxQueueCount is not null)
                peerAttrs.Writer.Write(RouteLinkAttributes.NumRxQueues, rxQueueCount.Value);
            if (txQueueCount is not null)
                peerAttrs.Writer.Write(RouteLinkAttributes.NumTxQueues, txQueueCount.Value);
        }
        Post(buffer, writer);
    }

    public void CreateBridge(string name, int? rxQueueCount = null, int? txQueueCount = null)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = BeginCreateLink(buffer, name, rxQueueCount, txQueueCount);
        using (var infoAttrs = writer.Attributes.WriteNested<RouteLinkInfoAttributes>(RouteLinkAttributes.LinkInfo))
            infoAttrs.Writer.Write(RouteLinkInfoAttributes.Kind, "bridge");
        Post(buffer, writer);
    }

    public void MoveTo(int index, NetNs ns)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteLinkMessage, RouteLinkAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.NewLink;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Ack;
        writer.Header.Index = index;
        writer.Attributes.Write(RouteLinkAttributes.NetNsFd, ns.Descriptor);
        Post(buffer, writer);
    }

    private RouteNetlinkMessageWriter<RouteLinkMessage, RouteLinkAttributes> BeginCreateLink(Span<byte> buffer, string name, int? rxQueueCount, int? txQueueCount)
    {
        var writer = GetWriter<RouteLinkMessage, RouteLinkAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.NewLink;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Create | NetlinkMessageFlags.Exclusive | NetlinkMessageFlags.Ack;
        writer.Attributes.Write(RouteLinkAttributes.Name, name);
        if (rxQueueCount is not null)
            writer.Attributes.Write(RouteLinkAttributes.NumRxQueues, rxQueueCount.Value);
        if (txQueueCount is not null)
            writer.Attributes.Write(RouteLinkAttributes.NumTxQueues, txQueueCount.Value);
        return writer;
    }

    private static LinkInformation ParseLink(RouteNetlinkMessage<RouteLinkMessage, RouteLinkAttributes> message)
    {
        var ifIndex = message.Header.Index;
        var up = message.Header.Flags.HasFlag(NetDeviceFlags.Up);
        string? name = null;
        MACAddress? macAddress = null;
        int? masterIndex = null;
        var rxQueueCount = 0;
        var txQueueCount = 0;
        foreach (var attribute in message.Attributes)
        {
            switch (attribute.Name)
            {
                case RouteLinkAttributes.Name:
                    name = attribute.AsString();
                    break;
                case RouteLinkAttributes.Address:
                    macAddress = attribute.AsValue<MACAddress>();
                    break;
                case RouteLinkAttributes.Master:
                    masterIndex = attribute.AsValue<int>();
                    break;
                case RouteLinkAttributes.NumRxQueues:
                    rxQueueCount = attribute.AsValue<int>();
                    break;
                case RouteLinkAttributes.NumTxQueues:
                    txQueueCount = attribute.AsValue<int>();
                    break;
            }
        }
        return name is null
            ? throw new InvalidOperationException($"Link with index '{ifIndex}' is missing a name attribute.")
            : new LinkInformation(ifIndex, name, up, macAddress, masterIndex, rxQueueCount, txQueueCount);
    }

    #endregion

    #region Addresses

    public LinkAddress[] GetAddresses(int linkIndex, AddressFamily addressFamily = AddressFamily.Unspecified)
    {
        if (addressFamily == AddressFamily.Unspecified)
            return [.. GetAddresses(linkIndex, AddressFamily.InterNetwork), .. GetAddresses(linkIndex, AddressFamily.InterNetworkV6)];

        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Large);
        var writer = GetWriter<RouteAddressMessage, RouteAddressAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.GetAddress;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Dump;
        writer.Header.LinkIndex = (uint)linkIndex;
        writer.Header.Family = ToLinuxAddressFamily(addressFamily);
        var addresses = new List<LinkAddress>();
        foreach (var message in Get(buffer, writer))
            if (message.Type == RouteNetlinkMessageType.NewAddress)
            {
                var prefixLength = message.Header.PrefixLength;
                IPAddress? address = null;
                RouteAddressFlags flags = default;
                foreach (var attribute in message.Attributes)
                    switch (attribute.Name)
                    {
                        case RouteAddressAttributes.Address:
                            address = new IPAddress(attribute.Data);
                            break;
                        case RouteAddressAttributes.Flags:
                            flags = attribute.AsValue<RouteAddressFlags>();
                            break;
                    }
                if (address is null)
                    throw new InvalidOperationException($"Address on link with index '{linkIndex}' is missing an address attribute");
                addresses.Add(new LinkAddress(address, prefixLength, flags.HasFlag(RouteAddressFlags.NoDad)));
            }
        return [.. addresses];
    }

    public void AddAddress(int linkIndex, LinkAddress address)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteAddressMessage, RouteAddressAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.NewAddress;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Create | NetlinkMessageFlags.Exclusive | NetlinkMessageFlags.Ack;
        WriteAddress(writer, linkIndex, address);
        Post(buffer, writer);
    }

    public void DeleteAddress(int linkIndex, LinkAddress address)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteAddressMessage, RouteAddressAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.DeleteAddress;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Ack;
        WriteAddress(writer, linkIndex, address);
        Post(buffer, writer);
    }

    private static void WriteAddress(RouteNetlinkMessageWriter<RouteAddressMessage, RouteAddressAttributes> writer, int linkIndex, LinkAddress address)
    {
        writer.Header.LinkIndex = (uint)linkIndex;
        writer.Header.PrefixLength = address.PrefixLength;
        writer.Header.Family = ToLinuxAddressFamily(address.AddressFamily);
        var size = GetAddressSize(address.AddressFamily);
        var localBytes = writer.Attributes.PrepareWrite(RouteAddressAttributes.Local, size);
        address.Address.TryWriteBytes(localBytes, out _);
        var addressBytes = writer.Attributes.PrepareWrite(RouteAddressAttributes.Address, size);
        localBytes.CopyTo(addressBytes);
        if (address.NoDad)
        {
            writer.Header.Flags |= RouteAddressFlags.NoDad;
            writer.Attributes.Write(RouteAddressAttributes.Flags, RouteAddressFlags.NoDad);
        }
    }

    #endregion

    #region Routes

    public RouteInformation[] GetRoutes(AddressFamily addressFamily = AddressFamily.Unspecified, uint? table = null, int? outputInterfaceIndex = null)
    {
        if (addressFamily == AddressFamily.Unspecified)
            return [.. GetRoutes(AddressFamily.InterNetwork, table, outputInterfaceIndex), .. GetRoutes(AddressFamily.InterNetworkV6, table, outputInterfaceIndex)];

        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Large);
        var writer = GetWriter<RouteMessage, RouteAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.GetRoute;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Dump;
        writer.Header.Family = ToLinuxAddressFamily(addressFamily);
        var routes = new List<RouteInformation>();
        foreach (var message in Get(buffer, writer))
            if (message.Type == RouteNetlinkMessageType.NewRoute)
            {
                var route = ParseRoute(message);
                if (table is not null && route.Table != table.Value)
                    continue;
                if (outputInterfaceIndex is not null && route.OutputInterfaceIndex != outputInterfaceIndex.Value)
                    continue;
                routes.Add(route);
            }
        return [.. routes];
    }

    public void AddRoute(RouteInformation route)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteMessage, RouteAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.NewRoute;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Create | NetlinkMessageFlags.Exclusive | NetlinkMessageFlags.Ack;
        WriteRoute(writer, route);
        Post(buffer, writer);
    }

    public void ReplaceRoute(RouteInformation route)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteMessage, RouteAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.NewRoute;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Create | NetlinkMessageFlags.Replace | NetlinkMessageFlags.Ack;
        WriteRoute(writer, route);
        Post(buffer, writer);
    }

    public void DeleteRoute(RouteInformation route)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteMessage, RouteAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.DeleteRoute;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Ack;
        WriteRoute(writer, route);
        Post(buffer, writer);
    }

    private static RouteInformation ParseRoute(RouteNetlinkMessage<RouteMessage, RouteAttributes> message)
    {
        var addressFamily = ToAddressFamily(message.Header.Family);
        RouteAddress? source = null;
        RouteAddress? destination = null;
        IPAddress? gateway = null;
        int? inputInterfaceIndex = null;
        int? outputInterfaceIndex = null;
        uint? priority = null;
        IPAddress? preferredSource = null;
        var table = (uint)message.Header.Table;
        RoutePreference? preference = null;
        foreach (var attribute in message.Attributes)
        {
            switch (attribute.Name)
            {
                case RouteAttributes.Source:
                    source = new RouteAddress(new IPAddress(attribute.Data), message.Header.SourceLength);
                    break;
                case RouteAttributes.Destination:
                    destination = new RouteAddress(new IPAddress(attribute.Data), message.Header.DestinationLength);
                    break;
                case RouteAttributes.Gateway:
                    gateway = new IPAddress(attribute.Data);
                    break;
                case RouteAttributes.InputInterface:
                    inputInterfaceIndex = attribute.AsValue<int>();
                    break;
                case RouteAttributes.OutputInterface:
                    outputInterfaceIndex = attribute.AsValue<int>();
                    break;
                case RouteAttributes.Priority:
                    priority = attribute.AsValue<uint>();
                    break;
                case RouteAttributes.PreferredSource:
                    preferredSource = new IPAddress(attribute.Data);
                    break;
                case RouteAttributes.Table:
                    table = attribute.AsValue<uint>();
                    break;
                case RouteAttributes.Preference:
                    preference = attribute.AsValue<RoutePreference>();
                    break;
            }
        }
        return new RouteInformation(addressFamily,
                                    source,
                                    destination,
                                    gateway,
                                    inputInterfaceIndex,
                                    outputInterfaceIndex,
                                    priority,
                                    preferredSource,
                                    table,
                                    preference,
                                    message.Header.Protocol,
                                    message.Header.Scope,
                                    message.Header.RouteType,
                                    message.Header.TypeOfService);
    }

    private static void WriteRoute(RouteNetlinkMessageWriter<RouteMessage, RouteAttributes> writer, RouteInformation route)
    {
        writer.Header.Family = ToLinuxAddressFamily(route.AddressFamily);
        writer.Header.SourceLength = route.Source?.PrefixLength ?? 0;
        writer.Header.DestinationLength = route.Destination?.PrefixLength ?? 0;
        writer.Header.Table = route.Table <= byte.MaxValue ? (byte)route.Table : (byte)RouteTable.Unspecified;
        writer.Header.Protocol = route.Protocol;
        writer.Header.Scope = route.Scope;
        writer.Header.RouteType = route.Type;
        writer.Header.TypeOfService = route.TypeOfService;

        if (route.Source is { } source)
            WriteAddress(writer.Attributes, RouteAttributes.Source, source.Address);
        if (route.Destination is { } destination)
            WriteAddress(writer.Attributes, RouteAttributes.Destination, destination.Address);
        if (route.Gateway is { } gateway)
            WriteAddress(writer.Attributes, RouteAttributes.Gateway, gateway);
        if (route.InputInterfaceIndex is { } inputInterfaceIndex)
            writer.Attributes.Write(RouteAttributes.InputInterface, inputInterfaceIndex);
        if (route.OutputInterfaceIndex is { } outputInterfaceIndex)
            writer.Attributes.Write(RouteAttributes.OutputInterface, outputInterfaceIndex);
        if (route.Priority is { } priority)
            writer.Attributes.Write(RouteAttributes.Priority, priority);
        if (route.PreferredSource is { } preferredSource)
            WriteAddress(writer.Attributes, RouteAttributes.PreferredSource, preferredSource);
        if (route.Table > byte.MaxValue)
            writer.Attributes.Write(RouteAttributes.Table, route.Table);
        if (route.Preference is { } preference)
            writer.Attributes.Write(RouteAttributes.Preference, preference);
    }

    private static void WriteAddress(NetlinkAttributeWriter<RouteAttributes> writer, RouteAttributes attribute, IPAddress address)
    {
        var bytes = writer.PrepareWrite(attribute, GetAddressSize(address.AddressFamily));
        address.TryWriteBytes(bytes, out _);
    }

    private static int GetAddressSize(AddressFamily addressFamily)
    {
        return addressFamily switch
        {
            AddressFamily.InterNetwork => 4,
            AddressFamily.InterNetworkV6 => 16,
            _ => throw new ArgumentException($"Unsupported address family: {addressFamily}", nameof(addressFamily))
        };
    }

    private static LinuxAddressFamily ToLinuxAddressFamily(AddressFamily addressFamily)
    {
        return addressFamily switch
        {
            AddressFamily.InterNetwork => LinuxAddressFamily.Inet,
            AddressFamily.InterNetworkV6 => LinuxAddressFamily.Inet6,
            _ => throw new ArgumentException($"Unsupported address family: {addressFamily}", nameof(addressFamily))
        };
    }

    private static AddressFamily ToAddressFamily(LinuxAddressFamily addressFamily)
    {
        return addressFamily switch
        {
            LinuxAddressFamily.Inet => AddressFamily.InterNetwork,
            LinuxAddressFamily.Inet6 => AddressFamily.InterNetworkV6,
            _ => throw new ArgumentException($"Unsupported address family: {addressFamily}", nameof(addressFamily))
        };
    }

    #endregion

    private RouteNetlinkMessageWriter<THeader, TAttr> GetWriter<THeader, TAttr>(Span<byte> buffer)
        where THeader : unmanaged
        where TAttr : unmanaged, Enum
    {
        return new RouteNetlinkMessageWriter<THeader, TAttr>(buffer)
        {
            PortId = PortId,
            Header = default
        };
    }

    private RouteNetlinkMessageCollection<THeader, TAttr> Get<THeader, TAttr>(Span<byte> buffer, RouteNetlinkMessageWriter<THeader, TAttr> message)
        where THeader : unmanaged
        where TAttr : unmanaged, Enum
    {
        return new(base.Get(buffer, message.Writer));
    }

    private void Post<THeader, TAttr>(Span<byte> buffer, RouteNetlinkMessageWriter<THeader, TAttr> message)
        where THeader : unmanaged
        where TAttr : unmanaged, Enum
    {
        base.Post(buffer, message.Writer);
    }
}