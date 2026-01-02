using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using NetlinkCore.Interop.Route;
using NetlinkCore.Protocol.Route;

using NetworkingPrimitivesCore;

namespace NetlinkCore;

public sealed class RouteNetlinkSocket() : NetlinkSocket(NetlinkFamily.Route)
{
    public Link GetLink(string name)
    {
        Unsafe.SkipInit<NetlinkBuffer>(out var buffer);
        var messageWriter = new RouteNetlinkMessageWriter<ifinfomsg, ifinfomsg_type, ifinfomsg_attrs>(buffer)
        {
            Type = ifinfomsg_type.RTM_GETLINK,
            Flags = NetlinkMessageFlags.Request,
            PortId = PortId,
            Header = default
        };
        messageWriter.Attributes.Write(ifinfomsg_attrs.IFLA_IFNAME, name);
        Send(messageWriter.Written);
        var receivedLength = Receive(buffer);
        var received = (ReadOnlySpan<byte>)buffer[..receivedLength];
        foreach (var message in new RouteNetlinkMessageCollection<ifinfomsg, ifinfomsg_type, ifinfomsg_attrs>(received))
            if (message.Type == ifinfomsg_type.RTM_NEWLINK)
                return ParseLink(message);
        throw new InvalidOperationException($"Link with name '{name}' not found.");
    }

    public Link[] GetLinks()
    {
        Unsafe.SkipInit<NetlinkBuffer>(out var buffer);
        var messageWriter = new RouteNetlinkMessageWriter<ifinfomsg, ifinfomsg_type, ifinfomsg_attrs>(buffer)
        {
            Type = ifinfomsg_type.RTM_GETLINK,
            Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Dump,
            PortId = PortId,
            Header = default
        };
        Send(messageWriter.Written);
        var receivedLength = Receive(buffer);
        var received = (ReadOnlySpan<byte>)buffer[..receivedLength];
        var links = new List<Link>();
        foreach (var message in new RouteNetlinkMessageCollection<ifinfomsg, ifinfomsg_type, ifinfomsg_attrs>(received))
            if (message.Type == ifinfomsg_type.RTM_NEWLINK)
                links.Add(ParseLink(message));
        return [.. links];
    }

    private static Link ParseLink(RouteNetlinkMessage<ifinfomsg, ifinfomsg_type, ifinfomsg_attrs> message)
    {
        var ifIndex = message.Header.ifi_index;
        string? name = null;
        MACAddress? macAddress = null;
        foreach (var attribute in message.Attributes)
        {
            switch (attribute.Name)
            {
                case ifinfomsg_attrs.IFLA_IFNAME:
                    name = attribute.AsString();
                    break;
                case ifinfomsg_attrs.IFLA_ADDRESS:
                    macAddress = attribute.AsValue<MACAddress>();
                    break;
            }
        }
        return name is null
            ? throw new InvalidOperationException($"Link with index '{ifIndex}' is missing a name attribute.")
            : new Link(ifIndex, name, macAddress);
    }
}