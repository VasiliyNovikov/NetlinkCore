using System;
using System.Diagnostics.CodeAnalysis;

using NetlinkCore.Interop;

namespace NetlinkCore;

[SuppressMessage("Design", "CA1069:Enums values should not be duplicated")]
[Flags]
public enum NetlinkMessageFlags
{
    None = 0,
    Request = Constants.NLM_F_REQUEST,
    MultiPart = Constants.NLM_F_MULTI,
    Ack = Constants.NLM_F_ACK,
    Echo = Constants.NLM_F_ECHO,
    DumpInconsistent = Constants.NLM_F_DUMP_INTR,
    DumpFiltered = Constants.NLM_F_DUMP_FILTERED,

    Root = Constants.NLM_F_ROOT,
    Match = Constants.NLM_F_MATCH,
    Atomic = Constants.NLM_F_ATOMIC,
    Dump = Constants.NLM_F_DUMP,

    Replace = Constants.NLM_F_REPLACE,
    Exclusive = Constants.NLM_F_EXCL,
    Create = Constants.NLM_F_CREATE,
    Append = Constants.NLM_F_APPEND,

    NonRecursive = Constants.NLM_F_NONREC,

    Capped = Constants.NLM_F_CAPPED,
    ExtendedAck = Constants.NLM_F_ACK_TLVS
}