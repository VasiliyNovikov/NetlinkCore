using System.Linq;
using System.Net;
using System.Net.Sockets;

using LibNlCore.Links;
using LibNlCore.Route;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NetNsCore;

using NetworkingPrimitivesCore;

namespace LibNlCore.Tests;

[TestClass]
public class LinkTests
{
    [TestMethod]
    public void LinkCollection_Open_Close()
    {
        using var collection = new LinkCollection();
        Assert.IsNotNull(collection);
    }

    [TestMethod]
    public void LinkCollection_GetLink()
    {
        using var collection = new LinkCollection();
        var link = collection["lo"];
        Assert.IsGreaterThan(0, link.Index);
        Assert.AreEqual("lo", link.Name);
        Assert.AreEqual(default(MACAddress), link.MacAddress);

        var addrs = link.Addresses.OrderBy(a => a.AddressFamily).ToArray();
        Assert.HasCount(2, addrs);
        Assert.AreEqual(IPAddress.Loopback, addrs[0].Address);
        Assert.AreEqual(8, addrs[0].PrefixLength);

        Assert.AreEqual(IPAddress.IPv6Loopback, addrs[1].Address);
        Assert.AreEqual(128, addrs[1].PrefixLength);
    }

    [TestMethod]
    public void LinkCollection_GetLinks()
    {
        using var collection = new LinkCollection();
        var links = collection.ToArray();
        Assert.IsGreaterThan(1, links.Length);
        var lo = links.FirstOrDefault(l => l.Name == "lo");
        Assert.IsNotNull(lo);
        Assert.IsGreaterThan(0, lo.Index);
    }

    [TestMethod]
    public void BridgeLink_Create_Delete()
    {
        const string bridgeName = "test_br0";
        const string bridgeAddress = "10.0.10.1";
        const byte bridgeAddressPrefix = 30;

        using var collection = new LinkCollection();

        Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", bridgeName));

        var bridge = collection.CreateBridge(bridgeName);
        try
        {
            var linkInfo = Script.Exec("ip", "link", "show", bridgeName);
            Assert.AreNotEqual("", linkInfo);
            Assert.Contains(bridgeName, linkInfo);
            Assert.Contains("DOWN", linkInfo);

            Assert.IsGreaterThan(0, bridge.Index);
            Assert.AreEqual(bridgeName, bridge.Name);
            Assert.IsFalse(bridge.Up);
            //Assert.AreEqual(RtnlBridgePortState.Disabled, bridge.PortState);

            bridge.Addresses.Add(new(IPAddress.Parse(bridgeAddress), bridgeAddressPrefix));

            Assert.Contains($"{bridgeAddress}/{bridgeAddressPrefix}", Script.Exec("ip", "address", "show", bridgeName));

            bridge.Up = true;

            linkInfo = Script.Exec("ip", "link", "show", bridgeName);
            Assert.Contains("UP", linkInfo);

            //Assert.AreEqual(RtnlBridgePortState.Disabled, bridge.PortState);

            collection.Delete(bridge);

            Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", bridgeName));
        }
        finally
        {
            Script.ExecNoThrow("ip", "link", "del", bridgeName);
        }
    }

    [TestMethod]
    public void VEthLink_Create_Delete()
    {
        const string vethName = "test_veth0";
        const string vethPeerName = "test_veth1";
        const string vethAddress = "10.0.10.1/30";
        const string vethPeerAddress = "10.0.10.2/30";

        using var collection = new LinkCollection();

        Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethName));
        Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethPeerName));

        try
        {
            var (veth, vethPeer) = collection.CreateVEth(vethName, vethPeerName);

            var linkInfo = Script.Exec("ip", "link", "show", vethName);
            Assert.AreNotEqual("", linkInfo);
            Assert.Contains(vethName, linkInfo);
            Assert.Contains("veth", linkInfo);
            Assert.Contains("DOWN", linkInfo);

            var peerInfo = Script.Exec("ip", "link", "show", vethPeerName);
            Assert.AreNotEqual("", peerInfo);
            Assert.Contains(vethPeerName, peerInfo);
            Assert.Contains("veth", peerInfo);
            Assert.Contains("DOWN", peerInfo);

            Assert.IsGreaterThan(0, veth.Index);
            Assert.IsGreaterThan(0, vethPeer.Index);

            Assert.AreEqual(vethName, veth.Name);
            Assert.AreEqual(vethPeerName, vethPeer.Name);

            veth.Addresses.Add(LinkAddress.Parse(vethAddress));
            Assert.Contains(vethAddress, Script.Exec("ip", "address", "show", vethName));

            vethPeer.Addresses.Add(LinkAddress.Parse(vethPeerAddress));
            Assert.Contains(vethPeerAddress, Script.Exec("ip", "address", "show", vethPeerName));

            veth.Up = true;
            vethPeer.Up = true;

            Assert.Contains("UP", Script.Exec("ip", "address", "show", vethName));
            Assert.Contains("UP", Script.Exec("ip", "address", "show", vethPeerName));

            collection.Delete(veth);

            Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethName));
            Assert.ThrowsExactly<AssertFailedException>(() => Script.Exec("ip", "link", "show", vethPeerName));
        }
        finally
        {
            Script.ExecNoThrow("ip", "link", "del", vethName);
        }
    }

    [TestMethod]
    public void LinkRoutes_Add_Replace_Delete()
    {
        const string bridgeName = "test_rtbr0";
        const string nsName = "linkroutetestns";
        const uint table = 50003;
        var route = new RouteInformation(IPAnyNetwork.Parse("203.0.113.0/24"),
                                         priority: 100,
                                         preferredSource: IPAddress.Parse("192.0.2.3"),
                                         table: table,
                                         scope: RouteScope.Link);
        var replacement = new RouteInformation(IPAnyNetwork.Parse("203.0.113.0/24"),
                                               priority: 100,
                                               preferredSource: IPAddress.Parse("192.0.2.4"),
                                               table: table,
                                               scope: RouteScope.Link);

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var collection = new LinkCollection())
            {
                var bridge = collection.CreateBridge(bridgeName);
                try
                {
                    bridge.Up = true;
                    bridge.Addresses.Add(new LinkAddress(IPAddress.Parse("192.0.2.3"), 32));
                    bridge.Addresses.Add(new LinkAddress(IPAddress.Parse("192.0.2.4"), 32));
                    bridge.Routes.Add(route);

                    var routes = bridge.Routes.Get(AddressFamily.InterNetwork, table).Where(IsTestRoute).ToArray();
                    Assert.HasCount(1, routes);
                    Assert.AreEqual(bridge.Index, routes[0].OutputInterfaceIndex);
                    Assert.AreEqual(route.Priority, routes[0].Priority);
                    Assert.AreEqual(route.PreferredSource, routes[0].PreferredSource);
                    Assert.Contains($"203.0.113.0/24 dev {bridgeName}", Script.Exec("ip", "route", "show", "table", table.ToString(System.Globalization.CultureInfo.InvariantCulture)));

                    bridge.Routes.Replace(replacement);
                    routes = collection.Routes.Get(AddressFamily.InterNetwork, table).Where(IsTestRoute).ToArray();
                    Assert.HasCount(1, routes);
                    Assert.AreEqual(bridge.Index, routes[0].OutputInterfaceIndex);
                    Assert.AreEqual(replacement.Priority, routes[0].Priority);
                    Assert.AreEqual(replacement.PreferredSource, routes[0].PreferredSource);

                    bridge.Routes.Remove(replacement);
                    Assert.IsEmpty(collection.Routes.Get(AddressFamily.InterNetwork, table).Where(IsTestRoute));
                }
                finally
                {
                    Script.ExecNoThrow("ip", "route", "flush", "table", table.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    collection.Delete(bridge);
                }
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }

        return;

        static bool IsTestRoute(RouteInformation route) => route.Destination == IPAnyNetwork.Parse("203.0.113.0/24");
    }
}