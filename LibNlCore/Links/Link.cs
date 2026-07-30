using System;

using LibNlCore.Generic.EthTool;
using LibNlCore.Route;

using NetNsCore;

using NetworkingPrimitivesCore;

namespace LibNlCore.Links;

public class Link
{
    private readonly RouteNetlinkSocket _socket;
    private readonly Lazy<EthToolNetlinkSocket> _ethToolSocketLazy;
    private readonly NetNs _ns;
    private LinkInformation _info;

    public int Index => _info.Index;

    public string Name => _info.Name;

    public int RXQueueCount => _info.RXQueueCount;

    public int TXQueueCount => _info.TXQueueCount;

    public bool Up
    {
        get => _info.Up;
        set
        {
            if (_info.Up == value)
                return;

            var change = _info with { Up = value };
            _socket.UpdateLink(_info, change);
            _info = change;
        }
    }

    public Link? Master
    {
        get => _info.MasterIndex is { } masterIndex
            ? field ??= new Link(_socket, _ethToolSocketLazy, _ns, _socket.GetLink(masterIndex))
            : null;
        set
        {
            var masterIndex = value?.Index;
            if (_info.MasterIndex == masterIndex)
                return;

            var change = _info with { MasterIndex = masterIndex };
            _socket.UpdateLink(_info, change);
            field = value;
            _info = change;
        }
    }

    public MACAddress? MacAddress
    {
        get => _info.MacAddress;
        set
        {
            if (_info.MacAddress == value)
                return;

            var change = _info with { MacAddress = value };
            _socket.UpdateLink(_info, change);
            _info = change;
        }
    }

    public LinkAddressCollection Addresses => field ??= new(_socket, Index);

    public LinkRouteCollection Routes => field ??= new(_socket, Index);

    public LinkFeatures Features => field ??= new(_ethToolSocketLazy.Value, Index);

    internal Link(RouteNetlinkSocket socket, Lazy<EthToolNetlinkSocket> ethToolSocketLazy, NetNs ns, LinkInformation info)
    {
        _socket = socket;
        _ethToolSocketLazy = ethToolSocketLazy;
        _ns = ns;
        _info = info;
    }

    public void MoveTo(NetNs ns) => _socket.MoveTo(_info.Index, ns);
}
