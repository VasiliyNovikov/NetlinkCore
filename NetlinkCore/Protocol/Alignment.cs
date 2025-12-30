namespace NetlinkCore.Protocol;

internal static class Alignment
{
    public static int Align(int length) => (length + 3) & ~3;
}