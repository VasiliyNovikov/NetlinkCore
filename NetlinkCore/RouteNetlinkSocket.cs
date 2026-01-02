using System;
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
        var receivedCount = Receive(buffer);
        var received = (ReadOnlySpan<byte>)buffer[..receivedCount];
        foreach (var message in new RouteNetlinkMessageCollection<ifinfomsg, ifinfomsg_type, ifinfomsg_attrs>(received))
        {
            var ifIndex = message.Header.ifi_index;
            MACAddress? macAddress = null;
            foreach (var attribute in message.Attributes)
            {
                Console.Error.WriteLine($"Attribute: {attribute.Name}, Length: {attribute.Data.Length}");
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
            return new Link(ifIndex, name, macAddress);
        }
        throw new InvalidOperationException($"Link with name '{name}' not found.");
    }
}