namespace NetlinkCore.Protocol;

internal static class Alignment
{
    public static uint Align(uint length) => (length + 3u) & ~3u;
}