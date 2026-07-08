using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

using LibNlCore.Route;

namespace LibNlCore.Links;

public sealed class LinkRouteCollection : IEnumerable<RouteInformation>
{
    private readonly RouteNetlinkSocket _socket;
    private readonly int _linkIndex;

    internal LinkRouteCollection(RouteNetlinkSocket socket, int linkIndex)
    {
        _socket = socket;
        _linkIndex = linkIndex;
    }

    public RouteInformation[] Get(AddressFamily addressFamily = AddressFamily.Unspecified, uint? table = null) => _socket.GetRoutes(addressFamily, table, _linkIndex);

    public void Add(RouteInformation route) => _socket.AddRoute(ForLink(route));

    public void Replace(RouteInformation route) => _socket.ReplaceRoute(ForLink(route));

    public void Remove(RouteInformation route) => _socket.DeleteRoute(ForLink(route));

    public IEnumerator<RouteInformation> GetEnumerator()
    {
        foreach (var route in _socket.GetRoutes(outputInterfaceIndex: _linkIndex))
            yield return route;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private RouteInformation ForLink(RouteInformation route)
    {
        if (route.OutputInterfaceIndex is { } outputInterfaceIndex && outputInterfaceIndex != _linkIndex)
            throw new ArgumentException($"Route output interface index must be {_linkIndex}.", nameof(route));
        return route.OutputInterfaceIndex == _linkIndex ? route : route.WithOutputInterfaceIndex(_linkIndex);
    }
}