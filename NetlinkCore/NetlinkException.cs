using LinuxCore;

namespace NetlinkCore;

public class NetlinkException(int error) : LinuxException((LinuxErrorNumber)(-error));