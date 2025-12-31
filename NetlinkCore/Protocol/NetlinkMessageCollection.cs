using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using NetlinkCore.Interop;

namespace NetlinkCore.Protocol;

internal readonly unsafe ref struct NetlinkMessageCollection(ReadOnlySpan<byte> messageBytes)
{
    private readonly ReadOnlySpan<byte> _messageBytes = messageBytes;

    public Enumerator GetEnumerator() => new(_messageBytes);

    public ref struct Enumerator
    {
        private SpanReader _reader;

        public NetlinkMessage Current { get; private set; }

        internal Enumerator(ReadOnlySpan<byte> messageBytes) => _reader = new SpanReader(messageBytes);

        public bool MoveNext()
        {
            if (_reader.IsEndOfBuffer)
                return false;

            ref readonly var header = ref _reader.Read<nlmsghdr>();
            var rawType = (NetlinkMessageType)header.nlmsg_type;
            var type = rawType & NetlinkMessageType.Mask;
            var subtype = (int)(rawType & ~NetlinkMessageType.Mask);
            var flags = (NetlinkMessageFlags)header.nlmsg_flags;
            var payload = _reader.Read((int)header.nlmsg_len - sizeof(nlmsghdr));

            if (type == NetlinkMessageType.Error)
            {
                var error = Unsafe.As<byte, nlmsgerr>(ref MemoryMarshal.GetReference(payload)).error;
                return error == 0
                    ? MoveNext()
                    : throw new NetlinkException(error);
            }

            Current = new(type, subtype, flags, payload);
            return true;
        }
    }
}