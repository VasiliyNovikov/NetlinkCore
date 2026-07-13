using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

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
        WriteRoute(writer, route, RouteOperation.Add);
        Post(buffer, writer);
    }

    public void ReplaceRoute(RouteInformation route)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteMessage, RouteAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.NewRoute;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Create | NetlinkMessageFlags.Replace | NetlinkMessageFlags.Ack;
        WriteRoute(writer, route, RouteOperation.Replace);
        Post(buffer, writer);
    }

    public void DeleteRoute(RouteInformation route)
    {
        using var buffer = new NetlinkBuffer(NetlinkBufferSize.Small);
        var writer = GetWriter<RouteMessage, RouteAttributes>(buffer);
        writer.Type = RouteNetlinkMessageType.DeleteRoute;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Ack;
        WriteRoute(writer, route, RouteOperation.Delete);
        Post(buffer, writer);
    }

    private static RouteInformation ParseRoute(RouteNetlinkMessage<RouteMessage, RouteAttributes> message)
    {
        var addressFamily = ToAddressFamily(message.Header.Family);
        IPAnyNetwork? source = null;
        IPAnyNetwork? destination = null;
        IPAddress? gateway = null;
        int? inputInterfaceIndex = null;
        int? outputInterfaceIndex = null;
        uint? priority = null;
        IPAddress? preferredSource = null;
        var table = (uint)message.Header.Table;
        RoutePreference? preference = null;
        RouteMetrics? metrics = null;
        foreach (var attribute in message.Attributes)
        {
            switch (attribute.Name)
            {
                case RouteAttributes.Source:
                    source = new IPAnyNetwork(new IPAnyAddress(attribute.Data), message.Header.SourceLength, false);
                    break;
                case RouteAttributes.Destination:
                    destination = new IPAnyNetwork(new IPAnyAddress(attribute.Data), message.Header.DestinationLength, false);
                    break;
                case RouteAttributes.Gateway:
                    gateway = new IPAddress(attribute.Data);
                    break;
                case RouteAttributes.Via:
                    gateway = new IPAddress(attribute.Data[sizeof(ushort)..]);
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
                case RouteAttributes.Metrics:
                    metrics = ParseRouteMetrics(attribute.AsNested<RouteMetricAttributes>());
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
                                    message.Header.TypeOfService,
                                    metrics);
    }

    private static void WriteRoute(RouteNetlinkMessageWriter<RouteMessage, RouteAttributes> writer, RouteInformation route, RouteOperation operation)
    {
        ValidateRoute(route, operation);

        writer.Header.Family = ToLinuxAddressFamily(route.AddressFamily);
        writer.Header.SourceLength = route.Source?.Prefix ?? 0;
        writer.Header.DestinationLength = route.Destination?.Prefix ?? 0;
        writer.Header.Table = route.Table <= byte.MaxValue ? (byte)route.Table : (byte)RouteTable.Unspecified;
        writer.Header.Protocol = route.Protocol;
        writer.Header.Scope = route.Scope;
        writer.Header.RouteType = route.Type;
        writer.Header.TypeOfService = route.TypeOfService;

        if (route.Source is { } source)
            writer.Attributes.Write(RouteAttributes.Source, source.Address.Bytes);
        if (route.Destination is { } destination)
            writer.Attributes.Write(RouteAttributes.Destination, destination.Address.Bytes);
        if (route.Gateway is { } gateway)
            if (gateway.AddressFamily == route.AddressFamily)
                writer.Attributes.Write(RouteAttributes.Gateway, ((IPAnyAddress)gateway).Bytes);
            else
            {
                var data = writer.Attributes.PrepareWrite(RouteAttributes.Via, sizeof(ushort) + GetAddressSize(gateway.AddressFamily));
                MemoryMarshal.Write(data, (ushort)ToLinuxAddressFamily(gateway.AddressFamily));
                gateway.TryWriteBytes(data[sizeof(ushort)..], out _);
            }
        if (route.InputInterfaceIndex is { } inputInterfaceIndex)
            writer.Attributes.Write(RouteAttributes.InputInterface, inputInterfaceIndex);
        if (route.OutputInterfaceIndex is { } outputInterfaceIndex)
            writer.Attributes.Write(RouteAttributes.OutputInterface, outputInterfaceIndex);
        if (route.Priority is { } priority)
            writer.Attributes.Write(RouteAttributes.Priority, priority);
        if (route.PreferredSource is { } preferredSource)
            writer.Attributes.Write(RouteAttributes.PreferredSource, ((IPAnyAddress)preferredSource).Bytes);
        if (route.Table > byte.MaxValue)
            writer.Attributes.Write(RouteAttributes.Table, route.Table);
        if (route.Preference is { } preference)
            writer.Attributes.Write(RouteAttributes.Preference, preference);
        if (route.Metrics is { IsEmpty: false } metrics)
        {
            using var metricAttributes = writer.Attributes.WriteNested<RouteMetricAttributes>(RouteAttributes.Metrics);
            WriteRouteMetrics(metricAttributes.Writer, metrics);
        }
    }

    private static void ValidateRoute(RouteInformation route, RouteOperation operation)
    {
        ArgumentNullException.ThrowIfNull(route);

        var isReplace = operation == RouteOperation.Replace;
        var isDelete = operation == RouteOperation.Delete;
        if (route.Source is { } source && operation != RouteOperation.Add)
        {
            if (route.AddressFamily != AddressFamily.InterNetwork && IsMismatchedPrefixSilentlyAccepted(route.AddressFamily, source))
                throw new ArgumentException($"Route source family {source.Address.AddressFamily} does not match {route.AddressFamily}; Linux would reinterpret its bytes.", nameof(route));
        }

        if (operation != RouteOperation.Add && route.Destination is { } destination && IsMismatchedPrefixSilentlyAccepted(route.AddressFamily, destination))
            throw new ArgumentException($"Route destination family {destination.Address.AddressFamily} does not match {route.AddressFamily}; Linux would reinterpret its bytes.", nameof(route));

        if (isDelete && route.AddressFamily == AddressFamily.InterNetwork && route.PreferredSource is { } preferredSource)
        {
            if (IsUnspecifiedAddress(preferredSource))
                throw new ArgumentException($"Linux treats preferred source {preferredSource} as unspecified; omit PreferredSource.", nameof(route));
            if (preferredSource.AddressFamily == AddressFamily.InterNetworkV6)
                throw new ArgumentException("Route preferred-source family does not match InterNetwork; Linux would truncate its bytes.", nameof(route));
        }

        if (isDelete && route.AddressFamily == AddressFamily.InterNetwork && route.Gateway is { AddressFamily: AddressFamily.InterNetwork } gateway && IsUnspecifiedAddress(gateway))
            throw new ArgumentException($"Linux treats gateway {gateway} as a direct or unspecified route selector; omit Gateway.", nameof(route));

        if (route.Gateway is { AddressFamily: AddressFamily.InterNetworkV6, ScopeId: not 0 } scopedGateway)
        {
            if (scopedGateway.ScopeId > int.MaxValue || route.OutputInterfaceIndex != (int)scopedGateway.ScopeId)
                throw new ArgumentException($"IPv6 gateway scope ID {scopedGateway.ScopeId} is not encoded on the wire; use the same OutputInterfaceIndex.", nameof(route));
        }

        if (isDelete && route.OutputInterfaceIndex == 0)
            throw new ArgumentException("Linux treats output interface index 0 as unspecified; omit OutputInterfaceIndex or use a nonzero index.", nameof(route));

        if (operation != RouteOperation.Add && route.Table == RouteTable.Unspecified)
            throw new ArgumentException("Linux resolves route table 0 to the main table; specify RouteTable.Main explicitly.", nameof(route));

        if (route.Priority == 0 && (isDelete || isReplace && route.AddressFamily == AddressFamily.InterNetworkV6))
            throw new ArgumentException(isDelete
                                            ? "Linux treats route priority 0 as a wildcard when deleting routes; omit Priority."
                                            : "Linux replaces IPv6 route priority 0 with 1024, changing the replacement key; omit Priority.",
                                        nameof(route));

        if (isDelete)
        {
            if (route.Protocol == RouteProtocol.Unspecified)
                throw new ArgumentException("Linux treats protocol 0 as a wildcard when deleting routes; specify Protocol.", nameof(route));
            if (route.AddressFamily == AddressFamily.InterNetwork && route.Scope == RouteScope.NoWhere)
                throw new ArgumentException("Linux treats scope Nowhere as a wildcard when deleting IPv4 routes; specify Scope.", nameof(route));
            if (route.AddressFamily == AddressFamily.InterNetwork && route.Type == RouteType.Unspecified)
                throw new ArgumentException("Linux treats route type Unspecified as a wildcard when deleting IPv4 routes; specify Type.", nameof(route));
        }

        if (isDelete && route.AddressFamily == AddressFamily.InterNetwork && route.Metrics is { } metrics)
        {
            ValidateDeleteMetricTime(route, metrics.RoundTripTime, RouteMetrics.RoundTripTimeTicksPerUnit, nameof(RouteMetrics.RoundTripTime));
            ValidateDeleteMetricTime(route, metrics.RoundTripTimeVariance, RouteMetrics.RoundTripTimeVarianceTicksPerUnit, nameof(RouteMetrics.RoundTripTimeVariance));
            ValidateDeleteMetricTime(route, metrics.MinimumRetransmissionTime, RouteMetrics.MinimumRetransmissionTimeTicksPerUnit, nameof(RouteMetrics.MinimumRetransmissionTime));
            if (metrics.CongestionControlAlgorithm is not null)
                throw new ArgumentException("CongestionControlAlgorithm cannot safely be used as a deletion key because Linux maps unavailable names to the unset metric.", nameof(route));
        }
    }

    private static bool IsMismatchedPrefixSilentlyAccepted(AddressFamily routeFamily, IPAnyNetwork network)
    {
        if (network.Address.AddressFamily == routeFamily || network.Prefix is 0 or > 32)
            return false;
        if (routeFamily == AddressFamily.InterNetworkV6)
            return network.Address.AddressFamily == AddressFamily.InterNetwork;
        if (routeFamily != AddressFamily.InterNetwork || network.Address.AddressFamily != AddressFamily.InterNetworkV6)
            return false;

        return HasZeroHostBits(network.Address.Bytes[..4], network.Prefix);
    }

    private static bool IsUnspecifiedAddress(IPAddress address)
    {
        Span<byte> bytes = stackalloc byte[16];
        address.TryWriteBytes(bytes, out var bytesWritten);
        return bytes[..bytesWritten].IndexOfAnyExcept((byte)0) < 0;
    }

    private static bool HasZeroHostBits(ReadOnlySpan<byte> bytes, byte prefixLength)
    {
        var firstHostByte = prefixLength / 8;
        var prefixBitsInByte = prefixLength % 8;
        if (prefixBitsInByte != 0)
        {
            if ((bytes[firstHostByte] & (byte)(byte.MaxValue >> prefixBitsInByte)) != 0)
                return false;
            firstHostByte++;
        }
        return bytes[firstHostByte..].IndexOfAnyExcept((byte)0) < 0;
    }

    private static void ValidateDeleteMetricTime(RouteInformation route, TimeSpan? value, long ticksPerUnit, string metricName)
    {
        if (value is { } actual && actual.Ticks % ticksPerUnit != 0)
            throw new ArgumentException($"{metricName} is not exactly representable in the route deletion key.", nameof(route));
    }

    private enum RouteOperation
    {
        Add,
        Replace,
        Delete
    }

    private static RouteMetrics ParseRouteMetrics(NetlinkAttributeCollection<RouteMetricAttributes> attributes)
    {
        var locks = RouteMetricLocks.None;
        uint? mtu = null;
        uint? window = null;
        TimeSpan? roundTripTime = null;
        TimeSpan? roundTripTimeVariance = null;
        uint? slowStartThreshold = null;
        uint? congestionWindow = null;
        uint? advertisedMss = null;
        uint? reordering = null;
        uint? hopLimit = null;
        uint? initialCongestionWindow = null;
        var features = RouteMetricFeatures.None;
        TimeSpan? minimumRetransmissionTime = null;
        uint? initialReceiveWindow = null;
        uint? quickAck = null;
        string? congestionControlAlgorithm = null;
        uint? fastOpenNoCookie = null;

        foreach (var attribute in attributes)
            switch (attribute.Name)
            {
                case RouteMetricAttributes.Lock:
                    locks = attribute.AsValue<RouteMetricLocks>();
                    break;
                case RouteMetricAttributes.Mtu:
                    mtu = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.Window:
                    window = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.RoundTripTime:
                    roundTripTime = RouteMetrics.DecodeTimeSpan(attribute.AsValue<uint>(), RouteMetrics.RoundTripTimeTicksPerUnit);
                    break;
                case RouteMetricAttributes.RoundTripTimeVariance:
                    roundTripTimeVariance = RouteMetrics.DecodeTimeSpan(attribute.AsValue<uint>(), RouteMetrics.RoundTripTimeVarianceTicksPerUnit);
                    break;
                case RouteMetricAttributes.SlowStartThreshold:
                    slowStartThreshold = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.CongestionWindow:
                    congestionWindow = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.AdvertisedMss:
                    advertisedMss = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.Reordering:
                    reordering = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.HopLimit:
                    hopLimit = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.InitialCongestionWindow:
                    initialCongestionWindow = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.Features:
                    features = attribute.AsValue<RouteMetricFeatures>();
                    break;
                case RouteMetricAttributes.MinimumRetransmissionTime:
                    minimumRetransmissionTime = RouteMetrics.DecodeTimeSpan(attribute.AsValue<uint>(), RouteMetrics.MinimumRetransmissionTimeTicksPerUnit);
                    break;
                case RouteMetricAttributes.InitialReceiveWindow:
                    initialReceiveWindow = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.QuickAck:
                    quickAck = attribute.AsValue<uint>();
                    break;
                case RouteMetricAttributes.CongestionControlAlgorithm:
                    congestionControlAlgorithm = attribute.AsString();
                    break;
                case RouteMetricAttributes.FastOpenNoCookie:
                    fastOpenNoCookie = attribute.AsValue<uint>();
                    break;
            }

        return new RouteMetrics(locks,
                                mtu,
                                window,
                                roundTripTime,
                                roundTripTimeVariance,
                                slowStartThreshold,
                                congestionWindow,
                                advertisedMss,
                                reordering,
                                hopLimit,
                                initialCongestionWindow,
                                features,
                                minimumRetransmissionTime,
                                initialReceiveWindow,
                                quickAck,
                                congestionControlAlgorithm,
                                fastOpenNoCookie);
    }

    private static void WriteRouteMetrics(NetlinkAttributeWriter<RouteMetricAttributes> writer, RouteMetrics metrics)
    {
        if (metrics.Locks != RouteMetricLocks.None)
            writer.Write(RouteMetricAttributes.Lock, metrics.Locks);
        if (metrics.Mtu is { } mtu)
            writer.Write(RouteMetricAttributes.Mtu, mtu);
        if (metrics.Window is { } window)
            writer.Write(RouteMetricAttributes.Window, window);
        if (metrics.RoundTripTime is { } roundTripTime)
            writer.Write(RouteMetricAttributes.RoundTripTime, RouteMetrics.EncodeTimeSpan(roundTripTime, RouteMetrics.RoundTripTimeTicksPerUnit));
        if (metrics.RoundTripTimeVariance is { } roundTripTimeVariance)
            writer.Write(RouteMetricAttributes.RoundTripTimeVariance, RouteMetrics.EncodeTimeSpan(roundTripTimeVariance, RouteMetrics.RoundTripTimeVarianceTicksPerUnit));
        if (metrics.SlowStartThreshold is { } slowStartThreshold)
            writer.Write(RouteMetricAttributes.SlowStartThreshold, slowStartThreshold);
        if (metrics.CongestionWindow is { } congestionWindow)
            writer.Write(RouteMetricAttributes.CongestionWindow, congestionWindow);
        if (metrics.AdvertisedMss is { } advertisedMss)
            writer.Write(RouteMetricAttributes.AdvertisedMss, advertisedMss);
        if (metrics.Reordering is { } reordering)
            writer.Write(RouteMetricAttributes.Reordering, reordering);
        if (metrics.HopLimit is { } hopLimit)
            writer.Write(RouteMetricAttributes.HopLimit, hopLimit);
        if (metrics.InitialCongestionWindow is { } initialCongestionWindow)
            writer.Write(RouteMetricAttributes.InitialCongestionWindow, initialCongestionWindow);
        if (metrics.Features != RouteMetricFeatures.None)
            writer.Write(RouteMetricAttributes.Features, metrics.Features);
        if (metrics.MinimumRetransmissionTime is { } minimumRetransmissionTime)
            writer.Write(RouteMetricAttributes.MinimumRetransmissionTime, RouteMetrics.EncodeTimeSpan(minimumRetransmissionTime, RouteMetrics.MinimumRetransmissionTimeTicksPerUnit));
        if (metrics.InitialReceiveWindow is { } initialReceiveWindow)
            writer.Write(RouteMetricAttributes.InitialReceiveWindow, initialReceiveWindow);
        if (metrics.QuickAck is { } quickAck)
            writer.Write(RouteMetricAttributes.QuickAck, quickAck);
        if (metrics.CongestionControlAlgorithm is { } congestionControlAlgorithm)
            writer.Write(RouteMetricAttributes.CongestionControlAlgorithm, congestionControlAlgorithm);
        if (metrics.FastOpenNoCookie is { } fastOpenNoCookie)
            writer.Write(RouteMetricAttributes.FastOpenNoCookie, fastOpenNoCookie);
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
