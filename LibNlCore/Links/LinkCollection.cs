using System;
using System.Collections;
using System.Collections.Generic;

using LibNlCore.Generic.EthTool;
using LibNlCore.Route;

using NetNsCore;

namespace LibNlCore.Links;

public sealed class LinkCollection : IEnumerable<Link>, IDisposable
{
    private readonly RouteNetlinkSocket _routeSocket;
    private readonly Lazy<EthToolNetlinkSocket> _ethToolSocketLazy;
    private readonly NetNs _ns;

    public Link this[int index] => Create(_routeSocket.GetLink(index));
    public Link this[string name] => Create(_routeSocket.GetLink(name));
    public RouteCollection Routes => field ??= new(_routeSocket);

    public LinkCollection(NetNs? ns = null)
    {
        _ns = ns is null ? NetNs.OpenCurrent() : ns.Clone();
        using (_ns.Enter())
            _routeSocket = new RouteNetlinkSocket();
        _ethToolSocketLazy = new(() =>
        {
            using (_ns.Enter())
                return new EthToolNetlinkSocket();
        });
    }

    public void Dispose()
    {
        _ns.Dispose();
        _routeSocket.Dispose();
        if (_ethToolSocketLazy.IsValueCreated)
            _ethToolSocketLazy.Value.Dispose();
    }

    public IEnumerator<Link> GetEnumerator()
    {
        foreach (var info in _routeSocket.GetLinks())
            yield return Create(info);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public (Link Link, Link Peer) CreateVEth(string name, string peerName, int? rxQueueCount = null, int? txQueueCount = null)
    {
        _routeSocket.CreateVEth(name, peerName, rxQueueCount, txQueueCount);
        return (this[name], this[peerName]);
    }

    public Link CreateBridge(string name)
    {
        _routeSocket.CreateBridge(name);
        return this[name];
    }

    public void Delete(Link link) => _routeSocket.DeleteLink(link.Index);

    private Link Create(LinkInformation info) => new(_routeSocket, _ethToolSocketLazy, _ns, info);
}
