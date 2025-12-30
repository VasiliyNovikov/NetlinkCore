using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}