using System.Runtime.InteropServices;

namespace NetlinkCore.Interop.Route;

[StructLayout(LayoutKind.Sequential)]
internal struct rtattr
{
    public ushort rta_len;
    public ushort rta_type;
}