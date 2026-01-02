using System.Linq;

using LinuxCore;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NetworkingPrimitivesCore;

namespace NetlinkCore.Tests;

[TestClass]
public class NetlinkSocketTests
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
        Assert.IsGreaterThan(0, link.IfIndex);
        Assert.AreEqual(default(MACAddress), link.MacAddress);
    }

    [TestMethod]
    public void RouteNetlinkSocket_GetLinks()
    {
        using var socket = new RouteNetlinkSocket();
        var links = socket.GetLinks();
        Assert.IsGreaterThan(1, links.Length);
        var lo = links.Single(l => l.Name == "lo");
        Assert.AreEqual("lo", lo.Name);
        Assert.IsGreaterThan(0, lo.IfIndex);
        Assert.AreEqual(default(MACAddress), lo.MacAddress);
    }

    [TestMethod]
    public void RouteNetlinkSocket_GetNonExistingLink()
    {
        using var socket = new RouteNetlinkSocket();
        var error = Assert.ThrowsExactly<NetlinkException>(() => socket.GetLink("lo1234"));
        Assert.AreEqual(LinuxErrorNumber.NoSuchDevice, error.ErrorNumber);
    }
}