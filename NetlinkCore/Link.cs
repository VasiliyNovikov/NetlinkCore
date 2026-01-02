using NetworkingPrimitivesCore;

namespace NetlinkCore;

public class Link(int ifIndex, string name, MACAddress? macAddress)
{
    public int IfIndex => ifIndex;
    public string Name => name;
    public MACAddress? MacAddress => macAddress;
}