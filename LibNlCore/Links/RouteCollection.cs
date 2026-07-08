using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

using LibNlCore.Route;

namespace LibNlCore.Links;

public sealed class RouteCollection : IEnumerable<RouteInformation>
{
    private readonly RouteNetlinkSocket _socket;

    internal RouteCollection(RouteNetlinkSocket socket) => _socket = socket;

    public RouteInformation[] Get(AddressFamily addressFamily = AddressFamily.Unspecified, uint? table = null) => _socket.GetRoutes(addressFamily, table);

    public void Add(RouteInformation route) => _socket.AddRoute(route);

    public void Replace(RouteInformation route) => _socket.ReplaceRoute(route);

    public void Remove(RouteInformation route) => _socket.DeleteRoute(route);

    public IEnumerator<RouteInformation> GetEnumerator()
    {
        foreach (var route in _socket.GetRoutes())
            yield return route;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
