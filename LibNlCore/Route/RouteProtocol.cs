using System.Diagnostics.CodeAnalysis;

namespace LibNlCore.Route;

[SuppressMessage("Style", "IDE0055:Fix formatting")]
public enum RouteProtocol
{
    Unspecified         =   0, // RTPROT_UNSPEC
    Redirect            =   1, // RTPROT_REDIRECT
    Kernel              =   2, // RTPROT_KERNEL
    Boot                =   3, // RTPROT_BOOT
    Static              =   4, // RTPROT_STATIC
    Gated               =   8, // RTPROT_GATED
    RouterAdvertisement =   9, // RTPROT_RA
    Mrt                 =  10, // RTPROT_MRT
    Zebra               =  11, // RTPROT_ZEBRA
    Bird                =  12, // RTPROT_BIRD
    DnRouted            =  13, // RTPROT_DNROUTED
    Xorp                =  14, // RTPROT_XORP
    Ntk                 =  15, // RTPROT_NTK
    Dhcp                =  16, // RTPROT_DHCP
    MRouted             =  17, // RTPROT_MROUTED
    Keepalived          =  18, // RTPROT_KEEPALIVED
    Babel               =  42, // RTPROT_BABEL
    Ovn                 =  84, // RTPROT_OVN
    OpenR               =  99, // RTPROT_OPENR
    Bgp                 = 186, // RTPROT_BGP
    Isis                = 187, // RTPROT_ISIS
    Ospf                = 188, // RTPROT_OSPF
    Rip                 = 189, // RTPROT_RIP
    Eigrp               = 192  // RTPROT_EIGRP
}