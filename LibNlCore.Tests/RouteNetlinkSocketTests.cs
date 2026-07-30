using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using LibNlCore.Route;

using LinuxCore;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NetNsCore;

using NetworkingPrimitivesCore;

namespace LibNlCore.Tests;

[TestClass]
public class RouteNetlinkSocketTests
{
    [TestMethod]
    public void LinkAddress_Equality_Includes_NoDad()
    {
        var address = IPAddress.Parse("2001:db8::1");
        var addresses = new HashSet<LinkAddress>
        {
            new(address, 64),
            new(address, 64, true)
        };

        Assert.HasCount(2, addresses);
    }

    [TestMethod]
    public void RouteNetlinkSocket_Rejects_Dangerous_Key_Reinterpretation()
    {
        const string nsName = "routeguardtestns";

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                var error = Assert.ThrowsExactly<ArgumentException>(() => socket.ReplaceRoute(new RouteInformation(AddressFamily.InterNetworkV6,
                                                                                                                     source: IPAnyNetwork.Parse("192.0.2.0/24"))));
                Assert.Contains("source family", error.Message);

                Assert.ThrowsExactly<ArgumentException>(() => socket.AddRoute(new RouteInformation(AddressFamily.InterNetworkV6, source: IPAnyNetwork.Parse("192.0.2.0/24"))));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetworkV6, source: IPAnyNetwork.Parse("192.0.2.0/24"))));

                error = Assert.ThrowsExactly<ArgumentException>(() => socket.ReplaceRoute(new RouteInformation(AddressFamily.InterNetwork, destination: IPAnyNetwork.Parse("c633:6400::/24"))));
                Assert.Contains("destination family", error.Message);

                Assert.ThrowsExactly<ArgumentException>(() => socket.AddRoute(new RouteInformation(AddressFamily.InterNetworkV6, destination: IPAnyNetwork.Parse("192.0.2.0/24"))));

                error = Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetwork, preferredSource: IPAddress.Parse("::ffff:192.0.2.1"))));
                Assert.Contains("preferred-source family", error.Message);

                Assert.ThrowsExactly<ArgumentException>(() => socket.AddRoute(new RouteInformation(AddressFamily.InterNetwork, preferredSource: IPAddress.Parse("c000:201::"))));
                Assert.ThrowsExactly<ArgumentException>(() => socket.ReplaceRoute(new RouteInformation(AddressFamily.InterNetwork, preferredSource: IPAddress.Parse("c000:201::"))));

                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetworkV6,
                                                                                                        gateway: IPAddress.Parse("::%123"))));
                Assert.ThrowsExactly<ArgumentException>(() => socket.ReplaceRoute(new RouteInformation(AddressFamily.InterNetworkV6,
                                                                                                        gateway: IPAddress.Parse("fe80::1%123"),
                                                                                                        outputInterfaceIndex: 124)));
                Assert.ThrowsExactly<ArgumentException>(() => socket.AddRoute(new RouteInformation(AddressFamily.InterNetworkV6,
                                                                                                    gateway: IPAddress.Parse("fe80::1%123"),
                                                                                                    outputInterfaceIndex: 124)));

                error = Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetwork,
                                                                                                                outputInterfaceIndex: 0)));
                Assert.Contains("index 0", error.Message);

                Assert.ThrowsExactly<ArgumentException>(() => socket.ReplaceRoute(new RouteInformation(AddressFamily.InterNetwork,
                                                                                                        table: RouteTable.Unspecified)));
                Assert.ThrowsExactly<ArgumentException>(() => socket.ReplaceRoute(new RouteInformation(AddressFamily.InterNetworkV6,
                                                                                                        priority: 0)));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetwork,
                                                                                                        type: RouteType.Unspecified)));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetwork,
                                                                                                        protocol: RouteProtocol.Unspecified)));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetwork,
                                                                                                        scope: RouteScope.NoWhere)));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetwork,
                                                                                                        priority: 0)));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(new RouteInformation(AddressFamily.InterNetwork,
                                                                                                        metrics: new RouteMetrics(congestionControlAlgorithm: "not-real"))));
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_Rounds_And_Validates_Metric_TimeSpans_When_Writing()
    {
        const string nsName = "routemetrictimens";
        const string linkName = "routemetrictime";
        const uint table = 50013;
        var destination = IPAnyNetwork.Parse("198.51.118.0/24");
        var metrics = new RouteMetrics(roundTripTime: TimeSpan.FromTicks(1),
                                       roundTripTimeVariance: TimeSpan.FromTicks(1),
                                       minimumRetransmissionTime: TimeSpan.FromTicks(1));
        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                socket.CreateBridge(linkName);
                var link = socket.GetLink(linkName);
                socket.UpdateLink(link, link with { Up = true });
                socket.AddAddress(link.Index, new LinkAddress(IPAddress.Parse("198.18.1.1"), 32));
                var route = new RouteInformation(destination,
                                                 outputInterfaceIndex: link.Index,
                                                 priority: 100,
                                                 table: table,
                                                 scope: RouteScope.Link,
                                                 metrics: metrics);

                socket.AddRoute(route);
                var returnedRoute = socket.GetRoutes(AddressFamily.InterNetwork, table).Single(candidate => candidate.Destination == destination);
                Assert.AreEqual(TimeSpan.FromMicroseconds(125), returnedRoute.Metrics?.RoundTripTime);
                Assert.AreEqual(TimeSpan.FromMicroseconds(250), returnedRoute.Metrics?.RoundTripTimeVariance);
                Assert.AreEqual(TimeSpan.FromMilliseconds(1), returnedRoute.Metrics?.MinimumRetransmissionTime);

                socket.DeleteRoute(route);
                Assert.IsEmpty(socket.GetRoutes(AddressFamily.InterNetwork, table));

                var negative = new RouteInformation(destination,
                                                    outputInterfaceIndex: link.Index,
                                                    table: table,
                                                    scope: RouteScope.Link,
                                                    metrics: new RouteMetrics(roundTripTime: TimeSpan.FromTicks(-1)));
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => socket.AddRoute(negative));

                var tooLarge = new RouteInformation(destination,
                                                    outputInterfaceIndex: link.Index,
                                                    table: table,
                                                    scope: RouteScope.Link,
                                                    metrics: new RouteMetrics(minimumRetransmissionTime: TimeSpan.FromTicks(uint.MaxValue * TimeSpan.TicksPerMillisecond + 1)));
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => socket.AddRoute(tooLarge));
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_Defers_Rejected_Routes_And_Allows_Ignored_Fields()
    {
        const string nsName = "routevalidationns";

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                const string linkName = "routevalidtst";
                const uint table = 50005;
                socket.CreateBridge(linkName);
                var link = socket.GetLink(linkName);
                socket.UpdateLink(link, link with { Up = true });
                var route = new RouteInformation(IPAnyNetwork.Parse("198.51.100.0/24"),
                                                 source: IPAnyNetwork.Parse("192.0.2.0/24"),
                                                 inputInterfaceIndex: 12345,
                                                 outputInterfaceIndex: link.Index,
                                                 table: table,
                                                 scope: RouteScope.Link);
                socket.AddRoute(route);
                socket.ReplaceRoute(route);

                Assert.ThrowsExactly<NetlinkException>(() => socket.DeleteRoute(route.WithOutputInterfaceIndex(-1)));
                Assert.HasCount(1, socket.GetRoutes(AddressFamily.InterNetwork, table));
                socket.DeleteRoute(route);
                Assert.ThrowsExactly<NetlinkException>(() => socket.AddRoute(new RouteInformation(AddressFamily.InterNetworkV6,
                                                                                                    preferredSource: IPAddress.Parse("192.0.2.1"))));
                Assert.ThrowsExactly<NetlinkException>(() => socket.AddRoute(new RouteInformation(AddressFamily.InterNetworkV6,
                                                                                                    gateway: IPAddress.IPv6Any)));
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_Deletes_Routes_Returned_By_GetRoutes()
    {
        const string nsName = "routegetdeltestns";
        const string linkName = "routegetdel";
        const uint blackholeTable = 50006;
        const uint viaTable = 50007;
        var blackholeDestination = IPAnyNetwork.Parse("198.51.120.0/24");
        var viaDestination = IPAnyNetwork.Parse("198.51.121.0/24");
        var localDestination = IPAnyNetwork.Parse("2001:db8:120::1/128");

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                socket.CreateBridge(linkName);
                try
                {
                    var link = socket.GetLink(linkName);
                    socket.UpdateLink(link, link with { Up = true });
                    socket.AddAddress(link.Index, new LinkAddress(IPAddress.Parse("2001:db8:120::1"), 64, true));
                    Script.Exec("ip", "route", "add", "blackhole", "198.51.120.0/24", "table", blackholeTable.ToString(CultureInfo.InvariantCulture), "proto", "0", "scope", "nowhere");
                    socket.AddRoute(new RouteInformation(AddressFamily.InterNetwork,
                                                         outputInterfaceIndex: link.Index,
                                                         scope: RouteScope.Link));
                    socket.AddRoute(new RouteInformation(viaDestination,
                                                         gateway: IPAddress.Any,
                                                         outputInterfaceIndex: link.Index,
                                                         table: viaTable));

                    var blackhole = socket.GetRoutes(AddressFamily.InterNetwork, blackholeTable).Single(route => route.Destination == blackholeDestination);
                    Assert.AreEqual(RouteProtocol.Unspecified, blackhole.Protocol);
                    Assert.AreEqual(RouteScope.NoWhere, blackhole.Scope);
                    Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(blackhole));

                    var via = socket.GetRoutes(AddressFamily.InterNetwork, viaTable).Single(route => route.Destination == viaDestination);
                    Assert.AreEqual(IPAddress.Any, via.Gateway);
                    socket.DeleteRoute(via);

                    var local = socket.GetRoutes(AddressFamily.InterNetworkV6, RouteTable.Local).Single(route => route.Destination == localDestination);
                    Assert.AreEqual(0u, local.Priority);
                    Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(local));

                    Assert.HasCount(1, socket.GetRoutes(AddressFamily.InterNetwork, blackholeTable));
                    Assert.IsEmpty(socket.GetRoutes(AddressFamily.InterNetwork, viaTable));
                    Assert.IsTrue(socket.GetRoutes(AddressFamily.InterNetworkV6, RouteTable.Local).Any(route => route.Destination == localDestination));
                }
                finally
                {
                    Script.ExecNoThrow("ip", "route", "flush", "table", blackholeTable.ToString(CultureInfo.InvariantCulture));
                    Script.ExecNoThrow("ip", "route", "flush", "table", viaTable.ToString(CultureInfo.InvariantCulture));
                    socket.DeleteLink(linkName);
                }
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_Rejects_Ambiguous_IPv4_Deletion_Keys()
    {
        const string nsName = "routekeytestns";
        const uint table = 50008;
        var tableString = table.ToString(CultureInfo.InvariantCulture);
        var protocolDestination = IPAnyNetwork.Parse("198.51.110.0/24");
        var scopeDestination = IPAnyNetwork.Parse("198.51.111.0/24");
        var priorityDestination = IPAnyNetwork.Parse("198.51.112.0/24");
        var congestionControlDestination = IPAnyNetwork.Parse("198.51.113.0/24");

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                Script.Exec("ip", "route", "append", "blackhole", protocolDestination.ToString(), "table", tableString, "proto", "99", "metric", "10");
                Script.Exec("ip", "route", "append", "blackhole", protocolDestination.ToString(), "table", tableString, "proto", "0", "metric", "10");
                Script.Exec("ip", "route", "append", "blackhole", scopeDestination.ToString(), "table", tableString, "proto", "99", "scope", "global", "metric", "10");
                Script.Exec("ip", "route", "append", "blackhole", scopeDestination.ToString(), "table", tableString, "proto", "99", "scope", "nowhere", "metric", "10");
                Script.Exec("ip", "route", "append", "blackhole", priorityDestination.ToString(), "table", tableString, "proto", "99", "metric", "10");
                Script.Exec("ip", "route", "append", "blackhole", priorityDestination.ToString(), "table", tableString, "proto", "99", "metric", "20");
                Script.Exec("ip", "route", "add", "blackhole", congestionControlDestination.ToString(), "table", tableString, "proto", "99");

                var routes = socket.GetRoutes(AddressFamily.InterNetwork, table);
                var unspecifiedProtocol = routes.Single(route => route.Destination == protocolDestination && route.Protocol == RouteProtocol.Unspecified);
                var nowhereScope = routes.Single(route => route.Destination == scopeDestination && route.Scope == RouteScope.NoWhere);
                var zeroPriority = new RouteInformation(priorityDestination,
                                                        priority: 0,
                                                        table: table,
                                                        protocol: (RouteProtocol)99,
                                                        type: RouteType.Blackhole);
                var unknownCongestionControl = new RouteInformation(congestionControlDestination,
                                                                    table: table,
                                                                    protocol: (RouteProtocol)99,
                                                                    type: RouteType.Blackhole,
                                                                    metrics: new RouteMetrics(congestionControlAlgorithm: "not-real"));

                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(unspecifiedProtocol));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(nowhereScope));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(zeroPriority));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(unknownCongestionControl));

                routes = socket.GetRoutes(AddressFamily.InterNetwork, table);
                Assert.HasCount(2, routes.Where(route => route.Destination == protocolDestination));
                Assert.HasCount(2, routes.Where(route => route.Destination == scopeDestination));
                Assert.HasCount(2, routes.Where(route => route.Destination == priorityDestination));
                Assert.HasCount(1, routes.Where(route => route.Destination == congestionControlDestination));
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_Rejects_Ambiguous_IPv6_Deletion_Keys()
    {
        const string nsName = "route6keytestns";
        const string firstLinkName = "route6key0";
        const string secondLinkName = "route6key1";
        const uint table = 50012;
        var tableString = table.ToString(CultureInfo.InvariantCulture);
        var protocolDestination = IPAnyNetwork.Parse("2001:db8:118::/64");
        var priorityDestination = IPAnyNetwork.Parse("2001:db8:119::/64");

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                socket.CreateBridge(firstLinkName);
                socket.CreateBridge(secondLinkName);
                var firstLink = socket.GetLink(firstLinkName);
                var secondLink = socket.GetLink(secondLinkName);
                socket.UpdateLink(firstLink, firstLink with { Up = true });
                socket.UpdateLink(secondLink, secondLink with { Up = true });

                Script.Exec("ip", "-6", "route", "append", protocolDestination.ToString(), "dev", firstLinkName, "table", tableString, "proto", "99", "metric", "10");
                Script.Exec("ip", "-6", "route", "append", protocolDestination.ToString(), "dev", secondLinkName, "table", tableString, "proto", "0", "metric", "10");
                Script.Exec("ip", "-6", "route", "append", "blackhole", priorityDestination.ToString(), "table", tableString, "proto", "99", "metric", "10");
                Script.Exec("ip", "-6", "route", "append", "blackhole", priorityDestination.ToString(), "table", tableString, "proto", "99", "metric", "20");

                var routes = socket.GetRoutes(AddressFamily.InterNetworkV6, table);
                var unspecifiedProtocol = new RouteInformation(protocolDestination,
                                                               priority: 10,
                                                               table: table,
                                                               protocol: RouteProtocol.Unspecified);
                var zeroPriority = new RouteInformation(priorityDestination,
                                                        priority: 0,
                                                        table: table,
                                                        protocol: (RouteProtocol)99,
                                                        type: RouteType.Blackhole);

                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(unspecifiedProtocol));
                Assert.ThrowsExactly<ArgumentException>(() => socket.DeleteRoute(zeroPriority));

                routes = socket.GetRoutes(AddressFamily.InterNetworkV6, table);
                Assert.HasCount(2, routes.Where(route => route.Destination == protocolDestination));
                Assert.HasCount(2, routes.Where(route => route.Destination == priorityDestination));
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_Leaves_Unsupported_Routes_ReadOnly()
    {
        const string nsName = "routeunsupportedns";
        const string firstLinkName = "routeunsup0";
        const string secondLinkName = "routeunsup1";
        const uint table = 50009;
        var tableString = table.ToString(CultureInfo.InvariantCulture);
        var nextHopDestination = IPAnyNetwork.Parse("198.51.114.0/24");
        var multipathDestination = IPAnyNetwork.Parse("198.51.115.0/24");
        var realmDestination = IPAnyNetwork.Parse("198.51.117.0/24");

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                socket.CreateBridge(firstLinkName);
                socket.CreateBridge(secondLinkName);
                var firstLink = socket.GetLink(firstLinkName);
                var secondLink = socket.GetLink(secondLinkName);
                socket.UpdateLink(firstLink, firstLink with { Up = true });
                socket.UpdateLink(secondLink, secondLink with { Up = true });

                Script.Exec("ip", "nexthop", "add", "id", "10", "dev", firstLinkName);
                Script.Exec("ip", "route", "add", nextHopDestination.ToString(), "table", tableString, "nhid", "10");
                Script.Exec("ip", "route", "add", multipathDestination.ToString(), "table", tableString,
                            "nexthop", "dev", firstLinkName, "weight", "1",
                            "nexthop", "dev", secondLinkName, "weight", "1");
                Script.Exec("ip", "route", "add", realmDestination.ToString(), "dev", firstLinkName, "table", tableString, "realm", "1");

                var routes = socket.GetRoutes(AddressFamily.InterNetwork, table);
                var nextHopRoute = routes.Single(route => route.Destination == nextHopDestination);
                var multipathRoute = routes.Single(route => route.Destination == multipathDestination);
                var realmRoute = routes.Single(route => route.Destination == realmDestination);
                Assert.IsFalse(nextHopRoute.CanModify);
                Assert.IsFalse(multipathRoute.CanModify);
                Assert.IsFalse(realmRoute.CanModify);
                Assert.IsFalse(multipathRoute.WithOutputInterfaceIndex(firstLink.Index).CanModify);

                Assert.ThrowsExactly<NotSupportedException>(() => socket.ReplaceRoute(nextHopRoute));
                Assert.ThrowsExactly<NotSupportedException>(() => socket.DeleteRoute(nextHopRoute));
                Assert.ThrowsExactly<NotSupportedException>(() => socket.ReplaceRoute(multipathRoute));
                Assert.ThrowsExactly<NotSupportedException>(() => socket.DeleteRoute(multipathRoute));
                Assert.ThrowsExactly<NotSupportedException>(() => socket.ReplaceRoute(realmRoute));
                Assert.ThrowsExactly<NotSupportedException>(() => socket.DeleteRoute(realmRoute));

                routes = socket.GetRoutes(AddressFamily.InterNetwork, table);
                Assert.HasCount(1, routes.Where(route => route.Destination == nextHopDestination));
                Assert.HasCount(1, routes.Where(route => route.Destination == multipathDestination));
                Assert.HasCount(1, routes.Where(route => route.Destination == realmDestination));
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }
    }

    [TestMethod]
    public void RouteNetlinkSocket_Parses_OnLink_As_ReadOnly()
    {
        const string nsName = "routeonlinktestns";
        const string linkName = "routeonlink";
        const uint table = 50010;
        var destination = IPAnyNetwork.Parse("198.51.116.0/24");
        var route = new RouteInformation(destination,
                                         gateway: IPAddress.Parse("192.0.2.1"),
                                         table: table,
                                         onLink: true);

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                socket.CreateBridge(linkName);
                var link = socket.GetLink(linkName);
                socket.UpdateLink(link, link with { Up = true });
                socket.AddAddress(link.Index, new LinkAddress(IPAddress.Parse("198.18.0.1"), 32));
                route = route.WithOutputInterfaceIndex(link.Index);

                socket.AddRoute(route);
                var returnedRoute = socket.GetRoutes(AddressFamily.InterNetwork, table).Single(candidate => candidate.Destination == destination);
                Assert.IsTrue(returnedRoute.OnLink);
                Assert.IsFalse(returnedRoute.CanModify);

                Assert.ThrowsExactly<NotSupportedException>(() => socket.ReplaceRoute(returnedRoute));
                Assert.ThrowsExactly<NotSupportedException>(() => socket.DeleteRoute(returnedRoute));
                Assert.HasCount(1, socket.GetRoutes(AddressFamily.InterNetwork, table).Where(candidate => candidate.Destination == destination));
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }
    }

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
        const string nsName = "route4testns";
        const uint table = 50001;
        var metrics = new RouteMetrics(locks: RouteMetricLocks.Mtu | RouteMetricLocks.MinimumRetransmissionTime | RouteMetricLocks.CongestionControlAlgorithm,
                                       mtu: 1400,
                                       window: 65535,
                                       roundTripTime: TimeSpan.FromMilliseconds(100),
                                       roundTripTimeVariance: TimeSpan.FromMilliseconds(50),
                                       slowStartThreshold: 32,
                                       congestionWindow: 16,
                                       advertisedMss: 1300,
                                       reordering: 4,
                                       hopLimit: 64,
                                       initialCongestionWindow: 12,
                                       features: RouteMetricFeatures.Ecn,
                                       minimumRetransmissionTime: TimeSpan.FromMilliseconds(200),
                                       initialReceiveWindow: 24,
                                       quickAck: 1,
                                       congestionControlAlgorithm: "reno",
                                       fastOpenNoCookie: 1);
        var route = new RouteInformation(IPAnyNetwork.Parse("198.51.100.0/24"),
                                         outputInterfaceIndex: null,
                                         priority: 100,
                                         preferredSource: IPAddress.Parse("192.0.2.1"),
                                         table: table,
                                         scope: RouteScope.Link,
                                         metrics: metrics);
        var replacement = new RouteInformation(IPAnyNetwork.Parse("198.51.100.0/24"),
                                               outputInterfaceIndex: null,
                                                priority: 100,
                                                preferredSource: IPAddress.Parse("192.0.2.2"),
                                                table: table,
                                                scope: RouteScope.Link,
                                                metrics: metrics);

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
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
                    Assert.Contains($"198.51.100.0/24 dev {name}", Script.Exec("ip", "route", "show", "table", table.ToString(CultureInfo.InvariantCulture)));

                    socket.ReplaceRoute(replacement);
                    routes = socket.GetRoutes(AddressFamily.InterNetwork, table).Where(IsTestRoute).ToArray();

                    Assert.HasCount(1, routes);
                    AssertRoute(replacement, routes[0]);

                    socket.DeleteRoute(routes[0]);
                    Assert.IsEmpty(socket.GetRoutes(AddressFamily.InterNetwork, table).Where(IsTestRoute));
                }
                finally
                {
                    Script.ExecNoThrow("ip", "route", "flush", "table", table.ToString(CultureInfo.InvariantCulture));
                    socket.DeleteLink(name);
                }
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }

        return;

        static bool IsTestRoute(RouteInformation route) => route.Destination == IPAnyNetwork.Parse("198.51.100.0/24");
    }

    [TestMethod]
    public void RouteNetlinkSocket_Add_Delete_IPv6_Route()
    {
        const string name = "brroute6tst";
        const string nsName = "route6testns";
        const uint table = 50002;
        var route = new RouteInformation(IPAnyNetwork.Parse("2001:db8:1234::/64"),
                                         outputInterfaceIndex: null,
                                         priority: 100,
                                         table: table,
                                         scope: RouteScope.Universe,
                                         metrics: new RouteMetrics(mtu: 1400));

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
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
                    Assert.Contains($"2001:db8:1234::/64 dev {name}", Script.Exec("ip", "-6", "route", "show", "table", table.ToString(CultureInfo.InvariantCulture)));

                    socket.DeleteRoute(routes[0]);
                    Assert.IsEmpty(socket.GetRoutes(AddressFamily.InterNetworkV6, table).Where(IsTestRoute));
                }
                finally
                {
                    Script.ExecNoThrow("ip", "-6", "route", "flush", "table", table.ToString(CultureInfo.InvariantCulture));
                    socket.DeleteLink(name);
                }
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }

        return;

        static bool IsTestRoute(RouteInformation route) => route.Destination == IPAnyNetwork.Parse("2001:db8:1234::/64");
    }

    [TestMethod]
    public void RouteNetlinkSocket_Add_Delete_IPv4_Route_Via_IPv6()
    {
        const string name = "brviatst";
        const string nsName = "routeviatestns";
        const uint table = 50004;
        var gateway = IPAddress.Parse("2001:db8:1::1");
        var route = new RouteInformation(IPAnyNetwork.Parse("198.51.101.0/24"),
                                         gateway: gateway,
                                         priority: 100,
                                         table: table);

        NetNs.Create(nsName);
        try
        {
            using var ns = NetNs.Open(nsName);
            using (ns.Enter())
            using (var socket = new RouteNetlinkSocket())
            {
                socket.CreateBridge(name);
                try
                {
                    var link = socket.GetLink(name);
                    socket.UpdateLink(link, link with { Up = true });
                    socket.AddAddress(link.Index, LinkAddress.Parse("2001:db8:1::2/64"));
                    route = route.WithOutputInterfaceIndex(link.Index);

                    socket.AddRoute(route);
                    var routes = socket.GetRoutes(AddressFamily.InterNetwork, table).Where(IsTestRoute).ToArray();

                    Assert.HasCount(1, routes);
                    AssertRoute(route, routes[0]);
                    Assert.Contains($"198.51.101.0/24 via inet6 {gateway} dev {name}", Script.Exec("ip", "route", "show", "table", table.ToString(CultureInfo.InvariantCulture)));

                    socket.DeleteRoute(route);
                    Assert.IsEmpty(socket.GetRoutes(AddressFamily.InterNetwork, table).Where(IsTestRoute));
                }
                finally
                {
                    Script.ExecNoThrow("ip", "route", "flush", "table", table.ToString(CultureInfo.InvariantCulture));
                    socket.DeleteLink(name);
                }
            }
        }
        finally
        {
            NetNs.Delete(nsName);
        }

        return;

        static bool IsTestRoute(RouteInformation route) => route.Destination == IPAnyNetwork.Parse("198.51.101.0/24");
    }

    private static void AssertRoute(RouteInformation expected, RouteInformation actual)
    {
        Assert.AreEqual(expected.AddressFamily, actual.AddressFamily);
        Assert.AreEqual(expected.Source, actual.Source);
        Assert.AreEqual(expected.Destination, actual.Destination);
        Assert.AreEqual(expected.Gateway, actual.Gateway);
        Assert.AreEqual(expected.InputInterfaceIndex, actual.InputInterfaceIndex);
        Assert.AreEqual(expected.OutputInterfaceIndex, actual.OutputInterfaceIndex);
        Assert.AreEqual(expected.Priority, actual.Priority);
        Assert.AreEqual(expected.PreferredSource, actual.PreferredSource);
        Assert.AreEqual(expected.Table, actual.Table);
        Assert.AreEqual(expected.Protocol, actual.Protocol);
        Assert.AreEqual(expected.Scope, actual.Scope);
        Assert.AreEqual(expected.Type, actual.Type);
        Assert.AreEqual(expected.TypeOfService, actual.TypeOfService);
        Assert.AreEqual(expected.OnLink, actual.OnLink);
        AssertRouteMetrics(expected.Metrics, actual.Metrics);
    }

    private static void AssertRouteMetrics(RouteMetrics? expected, RouteMetrics? actual)
    {
        if (expected is null)
        {
            Assert.IsNull(actual);
            return;
        }

        Assert.IsNotNull(actual);
        Assert.AreEqual(expected.Locks, actual.Locks);
        Assert.AreEqual(expected.Mtu, actual.Mtu);
        Assert.AreEqual(expected.Window, actual.Window);
        Assert.AreEqual(expected.RoundTripTime, actual.RoundTripTime);
        Assert.AreEqual(expected.RoundTripTimeVariance, actual.RoundTripTimeVariance);
        Assert.AreEqual(expected.SlowStartThreshold, actual.SlowStartThreshold);
        Assert.AreEqual(expected.CongestionWindow, actual.CongestionWindow);
        Assert.AreEqual(expected.AdvertisedMss, actual.AdvertisedMss);
        Assert.AreEqual(expected.Reordering, actual.Reordering);
        Assert.AreEqual(expected.HopLimit, actual.HopLimit);
        Assert.AreEqual(expected.InitialCongestionWindow, actual.InitialCongestionWindow);
        Assert.AreEqual(expected.Features, actual.Features);
        Assert.AreEqual(expected.MinimumRetransmissionTime, actual.MinimumRetransmissionTime);
        Assert.AreEqual(expected.InitialReceiveWindow, actual.InitialReceiveWindow);
        Assert.AreEqual(expected.QuickAck, actual.QuickAck);
        Assert.AreEqual(expected.CongestionControlAlgorithm, actual.CongestionControlAlgorithm);
        Assert.AreEqual(expected.FastOpenNoCookie, actual.FastOpenNoCookie);
    }
}
