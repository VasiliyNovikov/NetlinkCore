using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LibNlCore.Tests;

[TestClass]
public class TcpCongestionControlAlgorithmsTests
{
    [TestMethod]
    public void TcpCongestionControlAlgorithms_Exposes_Kernel_Algorithms()
    {
        Assert.Contains("reno", TcpCongestionControlAlgorithms.Available);
        Assert.DoesNotContain("nonexistent", TcpCongestionControlAlgorithms.Available);
        Assert.Contains("cubic", TcpCongestionControlAlgorithms.Available);
        Assert.IsTrue(TcpCongestionControlAlgorithms.IsAvailable("reno"));
        Assert.IsFalse(TcpCongestionControlAlgorithms.IsAvailable("nonexistent"));
        Assert.IsTrue(TcpCongestionControlAlgorithms.IsAvailable("cubic"));
    }
}