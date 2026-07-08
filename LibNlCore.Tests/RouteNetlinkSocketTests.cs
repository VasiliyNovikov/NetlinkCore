using System.Linq;
using System.Net;
using System.Net.Sockets;

using LinuxCore;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using LibNlCore.Route;

using NetNsCore;

using NetworkingPrimitivesCore;

namespace LibNlCore.Tests;

[TestClass]
public class RouteNetlinkSocketTests
{
    [TestMethod]
    public void RouteNetlinkSocket_Create()
    {
        using var socket = new RouteNetlinkSocket();
        Assert.AreNotEqual(0u, socket.PortId);
    }

    [TestMethod]
    public void RouteNetlinkSocket_GetLink()
    {
        using var socket = new RouteNetlinkSocket();
        var link = socket.GetLink("lo");
        Assert.IsNotNull(link);
        Assert.AreEqual("lo", link.Name);
        Assert.IsGreaterThan(0, link.Index);
        Assert.IsTrue(link.Up);
        Assert.IsNull(link.MasterIndex);
        Assert.AreEqual(default(MACAddress), link.MacAddress);

        var sameLink = socket.GetLink(link.Index);
        Assert.AreEqual(link.Name, sameLink.Name);
        Assert.AreEqual(link.Index, sameLink.Index);
        Assert.AreEqual(link.Up, sameLink.Up);
        Assert.AreEqual(link.MasterIndex, sameLink.MasterIndex);
        Assert.AreEqual(link.MacAddress, sameLink.MacAddress);
    }

    [TestMethod]
    public void RouteNetlinkSocket_GetLinks()
    {
        using var socket = new RouteNetlinkSocket();
        var links = socket.GetLinks();
        Assert.IsGreaterThan(1, links.Length);
        var lo = links.Single(l => l.Name == "lo");
        Assert.AreEqual("lo", lo.Name);
        Assert.IsGreaterThan(0, lo.Index);
        Assert.IsTrue(lo.Up);
        Assert.IsNull(lo.MasterIndex);
        Assert.AreEqual(default(MACAddress), lo.MacAddress);
    }

    [TestMethod]
    public void RouteNetlinkSocket_GetNonExistingLink()
    {
        using var socket = new RouteNetlinkSocket();
        var error = Assert.ThrowsExactly<NetlinkException>(() => socket.GetLink("lo1234"));
        Assert.AreEqual(LinuxErrorNumber.NoSuchDevice, error.ErrorNumber);
    }

    [TestMethod]
    public void RouteNetlinkSocket_Create_Delete_VEth()
    {
        using var socket = new RouteNetlinkSocket();
        const string name = "veth1test";
        const string peerName = "veth1ptest";
        const int queueCount = 3;

        socket.CreateVEth(name, peerName, queueCount, queueCount);

        var link = socket.GetLink(name);
        Assert.AreEqual(name, link.Name);
        Assert.AreEqual(queueCount, link.RXQueueCount);
        Assert.AreEqual(queueCount, link.TXQueueCount);
        Assert.IsGreaterThan(0, link.Index);
        Assert.IsFalse(link.Up);
        Assert.AreNotEqual(default(MACAddress), link.MacAddress);

        var peer = socket.GetLink(peerName);
        Assert.AreEqual(peerName, peer.Name);
        Assert.AreEqual(queueCount, peer.RXQueueCount);
        Assert.AreEqual(queueCount, peer.TXQueueCount);
        Assert.IsGreaterThan(0, peer.Index);
        Assert.IsFalse(peer.Up);
        Assert.AreNotEqual(default(MACAddress), peer.MacAddress);

        socket.DeleteLink(name);

        var error = Assert.ThrowsExactly<NetlinkException>(() => socket.GetLink(name));
        Assert.AreEqual(LinuxErrorNumber.NoSuchDevice, error.ErrorNumber);
        error = Assert.ThrowsExactly<NetlinkException>(() => socket.GetLink(peerName));
        Assert.AreEqual(LinuxErrorNumber.NoSuchDevice, error.ErrorNumber);
    }

    [TestMethod]
    public void RouteNetlinkSocket_Create_Delete_Bridge()
    {
        using var socket = new RouteNetlinkSocket();
        const string name = "br1test";
        var bridgeMac = MACAddress.Parse("02:12:34:56:78:9A");

        socket.CreateBridge(name);

        var link = socket.GetLink(name);
        Assert.AreEqual(name, link.Name);
        Assert.IsGreaterThan(0, link.Index);
        Assert.IsFalse(link.Up);
        Assert.AreNotEqual(default(MACAddress), link.MacAddress);

        var change = link with { MacAddress = bridgeMac, Up = true };
        socket.UpdateLink(link, change);

        link = socket.GetLink(name);
        Assert.AreEqual(bridgeMac, link.MacAddress);
        Assert.IsTrue(link.Up);

        socket.DeleteLink(link.Index);

        var error = Assert.ThrowsExactly<NetlinkException>(() => socket.GetLink(name));
        Assert.AreEqual(LinuxErrorNumber.NoSuchDevice, error.ErrorNumber);
    }

    [TestMethod]
    public void RouteNetlinkSocket_Set_Unset_Master()
    {
        using var socket = new RouteNetlinkSocket();
        const string brName = "br2test";
        const string vethName = "veth2test";
        const string vethPeerName = "veth2ptest";

        socket.CreateBridge(brName);
        socket.CreateVEth(vethName, vethPeerName);
        try
        {
            var bridge = socket.GetLink(brName);
            var veth = socket.GetLink(vethName);

            Assert.IsNull(veth.MasterIndex);
            Assert.IsNull(bridge.MasterIndex);

            var vethChange = veth with { MasterIndex = bridge.Index };
            socket.UpdateLink(veth, vethChange);

            veth = socket.GetLink(vethName);
            Assert.AreEqual(bridge.Index, veth.MasterIndex);

            vethChange = veth with { MasterIndex = null };
            socket.UpdateLink(veth, vethChange);

            veth = socket.GetLink(vethName);
            Assert.IsNull(veth.MasterIndex);
        }
        finally
        {
            socket.DeleteLink(vethName);
            socket.DeleteLink(brName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_MoveTo_NetNs()
    {
        using var socket = new RouteNetlinkSocket();
        const string name = "veth3test";
        const string peerName = "veth3ptest";
        const string nsName = "testns2";

        NetNs.Create(nsName);
        socket.CreateVEth(name, peerName);
        try
        {
            var link = socket.GetLink(name);
            using var ns = NetNs.Open(nsName);
            socket.MoveTo(link.Index, ns);
            var error = Assert.ThrowsExactly<NetlinkException>(() => socket.GetLink(name));
            Assert.AreEqual(LinuxErrorNumber.NoSuchDevice, error.ErrorNumber);
            using (ns.Enter())
            {
                using var nsSocket = new RouteNetlinkSocket();
                link = nsSocket.GetLink(name);
                Assert.AreEqual(name, link.Name);
            }
        }
        finally
        {
            socket.DeleteLink(peerName);
            NetNs.Delete(nsName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_GetAddresses()
    {
        using var socket = new RouteNetlinkSocket();
        var lo = socket.GetLink("lo");
        var loAddresses = socket.GetAddresses(lo.Index).OrderBy(a => a.AddressFamily).ToList();
        Assert.HasCount(2, loAddresses);
        var ipv4 = loAddresses[0];
        Assert.AreEqual(IPAddress.Loopback, ipv4.Address);
        Assert.AreEqual(8, ipv4.PrefixLength);
        Assert.IsFalse(ipv4.NoDad);
        var ipv6 = loAddresses[1];
        Assert.AreEqual(IPAddress.IPv6Loopback, ipv6.Address);
        Assert.AreEqual(128, ipv6.PrefixLength);
        Assert.IsFalse(ipv6.NoDad);
    }

    [TestMethod]
    public void RouteNetlinkSocket_Add_Delete_Address()
    {
        const string name = "braddrtst";
        var ipv4 = new LinkAddress(IPAddress.Parse("192.168.128.44"), 24);
        var ipv6 = new LinkAddress(IPAddress.Parse("2001:db8::4444"), 64, true);

        using var socket = new RouteNetlinkSocket();
        socket.CreateBridge(name);
        try
        {
            var link = socket.GetLink(name);
            Assert.IsEmpty(socket.GetAddresses(link.Index));

            socket.AddAddress(link.Index, ipv4);
            socket.AddAddress(link.Index, ipv6);

            var addresses = socket.GetAddresses(link.Index).OrderBy(a => a.AddressFamily).ToList();

            Assert.HasCount(2, addresses);
            var addr = addresses[0];
            Assert.AreEqual(ipv4.Address, addr.Address);
            Assert.AreEqual(ipv4.PrefixLength, addr.PrefixLength);
            Assert.AreEqual(ipv4.NoDad, addr.NoDad);

            addr = addresses[1];
            Assert.AreEqual(ipv6.Address, addr.Address);
            Assert.AreEqual(ipv6.PrefixLength, addr.PrefixLength);
            Assert.AreEqual(ipv6.NoDad, addr.NoDad);

            socket.DeleteAddress(link.Index, ipv4);
            socket.DeleteAddress(link.Index, ipv6);
            Assert.IsEmpty(socket.GetAddresses(link.Index));
        }
        finally
        {
            socket.DeleteLink(name);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_Add_Replace_Delete_IPv4_Route()
    {
        const string name = "brroute4tst";
        const uint table = 50001;
        var route = new RouteInformation(
            IPAddress.Parse("198.51.100.0"),
            24,
            outputInterfaceIndex: null,
            priority: 100,
            preferredSource: IPAddress.Parse("192.0.2.1"),
            table: table,
            scope: RouteScope.Link);
        var replacement = new RouteInformation(
            IPAddress.Parse("198.51.100.0"),
            24,
            outputInterfaceIndex: null,
            priority: 100,
            preferredSource: IPAddress.Parse("192.0.2.2"),
            table: table,
            scope: RouteScope.Link);

        using var socket = new RouteNetlinkSocket();
        socket.CreateBridge(name);
        try
        {
            var link = socket.GetLink(name);
            var linkChange = link with { Up = true };
            socket.UpdateLink(link, linkChange);
            socket.AddAddress(link.Index, new LinkAddress(IPAddress.Parse("192.0.2.1"), 32));
            socket.AddAddress(link.Index, new LinkAddress(IPAddress.Parse("192.0.2.2"), 32));
            route = route.WithOutputInterfaceIndex(link.Index);
            replacement = replacement.WithOutputInterfaceIndex(link.Index);

            socket.AddRoute(route);
            var routes = socket.GetRoutes(AddressFamily.InterNetwork, table).Where(IsTestRoute).ToArray();

            Assert.HasCount(1, routes);
            AssertRoute(route, routes[0]);
            Assert.Contains($"198.51.100.0/24 dev {name}", Script.Exec("ip", "route", "show", "table", table.ToString(System.Globalization.CultureInfo.InvariantCulture)));

            socket.ReplaceRoute(replacement);
            routes = socket.GetRoutes(AddressFamily.InterNetwork, table).Where(IsTestRoute).ToArray();

            Assert.HasCount(1, routes);
            AssertRoute(replacement, routes[0]);

            socket.DeleteRoute(replacement);
            Assert.IsEmpty(socket.GetRoutes(AddressFamily.InterNetwork, table).Where(IsTestRoute));
        }
        finally
        {
            Script.ExecNoThrow("ip", "route", "flush", "table", table.ToString(System.Globalization.CultureInfo.InvariantCulture));
            socket.DeleteLink(name);
        }

        static bool IsTestRoute(RouteInformation route) => route.Destination?.Equals(IPAddress.Parse("198.51.100.0")) == true;
    }

    [TestMethod]
    public void RouteNetlinkSocket_Add_Delete_IPv6_Route()
    {
        const string name = "brroute6tst";
        const uint table = 50002;
        var route = new RouteInformation(
            IPAddress.Parse("2001:db8:1234::"),
            64,
            outputInterfaceIndex: null,
            priority: 100,
            table: table,
            scope: RouteScope.Universe);

        using var socket = new RouteNetlinkSocket();
        socket.CreateBridge(name);
        try
        {
            var link = socket.GetLink(name);
            var linkChange = link with { Up = true };
            socket.UpdateLink(link, linkChange);
            route = route.WithOutputInterfaceIndex(link.Index);

            socket.AddRoute(route);
            var routes = socket.GetRoutes(AddressFamily.InterNetworkV6, table).Where(IsTestRoute).ToArray();

            Assert.HasCount(1, routes);
            AssertRoute(route, routes[0]);
            Assert.Contains($"2001:db8:1234::/64 dev {name}", Script.Exec("ip", "-6", "route", "show", "table", table.ToString(System.Globalization.CultureInfo.InvariantCulture)));

            socket.DeleteRoute(route);
            Assert.IsEmpty(socket.GetRoutes(AddressFamily.InterNetworkV6, table).Where(IsTestRoute));
        }
        finally
        {
            Script.ExecNoThrow("ip", "-6", "route", "flush", "table", table.ToString(System.Globalization.CultureInfo.InvariantCulture));
            socket.DeleteLink(name);
        }

        static bool IsTestRoute(RouteInformation route) => route.Destination?.Equals(IPAddress.Parse("2001:db8:1234::")) == true;
    }

    private static void AssertRoute(RouteInformation expected, RouteInformation actual)
    {
        Assert.AreEqual(expected.AddressFamily, actual.AddressFamily);
        Assert.AreEqual(expected.Destination, actual.Destination);
        Assert.AreEqual(expected.DestinationPrefixLength, actual.DestinationPrefixLength);
        Assert.AreEqual(expected.Gateway, actual.Gateway);
        Assert.AreEqual(expected.OutputInterfaceIndex, actual.OutputInterfaceIndex);
        Assert.AreEqual(expected.Priority, actual.Priority);
        Assert.AreEqual(expected.PreferredSource, actual.PreferredSource);
        Assert.AreEqual(expected.Table, actual.Table);
        Assert.AreEqual(expected.Protocol, actual.Protocol);
        Assert.AreEqual(expected.Scope, actual.Scope);
        Assert.AreEqual(expected.Type, actual.Type);
    }
}
