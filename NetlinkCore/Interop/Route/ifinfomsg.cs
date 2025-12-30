using System.Runtime.InteropServices;

namespace NetlinkCore.Interop.Route;

[StructLayout(LayoutKind.Sequential)]
internal struct ifinfomsg
{
    public byte ifi_family;
    public byte __ifi_pad;
    public ushort ifi_type; /* ARPHRD_* */
    public int ifi_index;   /* Link index	*/
    public uint ifi_flags;  /* IFF_* flags	*/
    public uint ifi_change; /* IFF_* change mask */
}