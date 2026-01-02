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
        GetBuffer(out var buffer);
        var writer = GetWriter<ifinfomsg, ifinfomsg_type, IFLA_ATTRS>(ref buffer);
        writer.Type = ifinfomsg_type.RTM_GETLINK;
        writer.Flags = NetlinkMessageFlags.Request;
        writer.Attributes.Write(IFLA_ATTRS.IFLA_IFNAME, name);
        foreach (var message in Get(ref buffer, writer))
            if (message.Type == ifinfomsg_type.RTM_NEWLINK)
                return ParseLink(message);
        throw new InvalidOperationException($"Link with name '{name}' not found.");
    }

    public Link[] GetLinks()
    {
        GetBuffer(out var buffer);
        var writer = GetWriter<ifinfomsg, ifinfomsg_type, IFLA_ATTRS>(ref buffer);
        writer.Type = ifinfomsg_type.RTM_GETLINK;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Dump;
        var links = new List<Link>();
        foreach (var message in Get(ref buffer, writer))
            if (message.Type == ifinfomsg_type.RTM_NEWLINK)
                links.Add(ParseLink(message));
        return [.. links];
    }

    public void DeleteLink(string name)
    {
        GetBuffer(out var buffer);
        var writer = GetWriter<ifinfomsg, ifinfomsg_type, IFLA_ATTRS>(ref buffer);
        writer.Type = ifinfomsg_type.RTM_DELLINK;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Ack;
        writer.Attributes.Write(IFLA_ATTRS.IFLA_IFNAME, name);
        Post(ref buffer, writer);
    }

    public void CreateVEth(string name, string peerName)
    {
        GetBuffer(out var buffer);
        var writer = GetWriter<ifinfomsg, ifinfomsg_type, IFLA_ATTRS>(ref buffer);
        writer.Type = ifinfomsg_type.RTM_NEWLINK;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Create | NetlinkMessageFlags.Exclusive | NetlinkMessageFlags.Ack;
        writer.Attributes.Write(IFLA_ATTRS.IFLA_IFNAME, name);
        using (var infoAttrs = writer.Attributes.WriteNested<IFLA_INFO_ATTRS>(IFLA_ATTRS.IFLA_LINKINFO))
        {
            infoAttrs.Writer.Write(IFLA_INFO_ATTRS.IFLA_INFO_KIND, "veth");
            using var vethAttrs = infoAttrs.Writer.WriteNested<VETH_INFO_ATTRS>(IFLA_INFO_ATTRS.IFLA_INFO_DATA);
            using var peerAttrs = vethAttrs.Writer.WriteNested<IFLA_ATTRS, ifinfomsg>(VETH_INFO_ATTRS.VETH_INFO_PEER);
            peerAttrs.Header = default;
            peerAttrs.Writer.Write(IFLA_ATTRS.IFLA_IFNAME, peerName);
        }
        Post(ref buffer, writer);
    }

    public void CreateBridge(string name)
    {
        GetBuffer(out var buffer);
        var writer = GetWriter<ifinfomsg, ifinfomsg_type, IFLA_ATTRS>(ref buffer);
        writer.Type = ifinfomsg_type.RTM_NEWLINK;
        writer.Flags = NetlinkMessageFlags.Request | NetlinkMessageFlags.Create | NetlinkMessageFlags.Exclusive | NetlinkMessageFlags.Ack;
        writer.PortId = PortId;
        writer.Attributes.Write(IFLA_ATTRS.IFLA_IFNAME, name);
        using (var infoAttrs = writer.Attributes.WriteNested<IFLA_INFO_ATTRS>(IFLA_ATTRS.IFLA_LINKINFO))
            infoAttrs.Writer.Write(IFLA_INFO_ATTRS.IFLA_INFO_KIND, "bridge");
        Post(ref buffer, writer);
    }

    private static Link ParseLink(RouteNetlinkMessage<ifinfomsg, ifinfomsg_type, IFLA_ATTRS> message)
    {
        var ifIndex = message.Header.ifi_index;
        string? name = null;
        MACAddress? macAddress = null;
        foreach (var attribute in message.Attributes)
        {
            switch (attribute.Name)
            {
                case IFLA_ATTRS.IFLA_IFNAME:
                    name = attribute.AsString();
                    break;
                case IFLA_ATTRS.IFLA_ADDRESS:
                    macAddress = attribute.AsValue<MACAddress>();
                    break;
            }
        }
        return name is null
            ? throw new InvalidOperationException($"Link with index '{ifIndex}' is missing a name attribute.")
            : new Link(ifIndex, name, macAddress);
    }

    private static void GetBuffer(out NetlinkBuffer buffer) => Unsafe.SkipInit(out buffer);

    private RouteNetlinkMessageWriter<THeader, TMsgType, TAttr> GetWriter<THeader, TMsgType, TAttr>(ref NetlinkBuffer buffer)
        where THeader : unmanaged
        where TMsgType : unmanaged, Enum
        where TAttr : unmanaged, Enum
    {
        return new RouteNetlinkMessageWriter<THeader, TMsgType, TAttr>(buffer)
        {
            PortId = PortId,
            Header = default
        };
    }

    private RouteNetlinkMessageCollection<THeader, TMsgType, TAttr> Get<THeader, TMsgType, TAttr>(ref NetlinkBuffer buffer, RouteNetlinkMessageWriter<THeader, TMsgType, TAttr> message)
        where THeader : unmanaged
        where TMsgType : unmanaged, Enum
        where TAttr : unmanaged, Enum
    {
        Send(message.Written);
        var receivedLength = Receive(buffer);
        var received = (ReadOnlySpan<byte>)buffer[..receivedLength];
        return new RouteNetlinkMessageCollection<THeader, TMsgType, TAttr>(received);
    }

    private void Post<THeader, TMsgType, TAttr>(ref NetlinkBuffer buffer, RouteNetlinkMessageWriter<THeader, TMsgType, TAttr> message)
        where THeader : unmanaged
        where TMsgType : unmanaged, Enum
        where TAttr : unmanaged, Enum
    {
        foreach (var _ in Get(ref buffer, message)) ;
    }
}