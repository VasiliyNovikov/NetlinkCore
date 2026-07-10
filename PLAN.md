# Route Management Review Plan

## High Priority

1. [ ] Preserve complete route identities.
   `RouteInformation` currently drops multipath nexthops, nexthop IDs, metrics, flags, and encapsulation. Prevent lossy dump results from being passed to mutation methods, or model and round-trip these attributes so removal cannot delete the wrong route or all ECMP nexthops. Update per-link queries to include ECMP routes.
   References: `LibNlCore/Route/RouteNetlinkSocket.cs:322-407`, `LibNlCore/Links/LinkRouteCollection.cs:21-32`

2. [ ] Validate route address families.
   Require source, destination, gateway, and preferred-source addresses to match the route family unless cross-family gateways are explicitly represented with `RTA_VIA`. This prevents malformed attributes from becoming unrelated routes.
   References: `LibNlCore/Route/RouteInformation.cs:6-52`, `LibNlCore/Route/RouteNetlinkSocket.cs:382-412`

3. [ ] Reject unsupported route selectors.
   Reject IPv4 source prefixes and mutation-time input-interface selectors instead of silently installing or deleting broader routes. Reject nonzero IPv6 TOS and require the supported IPv6 scope semantics.
   References: `LibNlCore/Route/RouteInformation.cs:7-20`, `LibNlCore/Route/RouteNetlinkSocket.cs:383-400`

4. [ ] Isolate privileged route tests.
   Stop flushing fixed host routing tables 50001-50003. Run route tests in dedicated network namespaces or delete only the exact routes created by each test.
   References: `LibNlCore.Tests/RouteNetlinkSocketTests.cs:308`, `LibNlCore.Tests/RouteNetlinkSocketTests.cs:349`, `LibNlCore.Tests/LinkTests.cs:204`

## Medium Priority

5. [ ] Require positive interface indices.
   Reject `InputInterfaceIndex` and `OutputInterfaceIndex` values less than one. Linux treats output interface zero as an unspecified wildcard during deletion, which can target another interface.
   References: `LibNlCore/Route/RouteNetlinkSocket.cs:312-319`, `LibNlCore/Route/RouteNetlinkSocket.cs:397-400`

6. [ ] Define consistent unspecified-table behavior.
   Normalize or reject `RouteTable.Unspecified`. Mutation currently maps table zero to `Main`, while `GetRoutes(table: 0)` filters for a table that will not be returned.
   References: `LibNlCore/Route/RouteNetlinkSocket.cs:283-285`, `LibNlCore/Route/RouteNetlinkSocket.cs:385-406`

7. [ ] Clarify replacement semantics.
   `ReplaceRoute` is an upsert and cannot replace a route while changing key fields such as priority. Rename or document it as `AddOrReplace`, or accept the original route key and perform a strict replacement.
   Reference: `LibNlCore/Route/RouteNetlinkSocket.cs:302-309`

8. [ ] Address the LinuxCore consumer API break.
   The upgrade from LinuxCore 0.1.15 to 0.3.10 makes inherited `SendTo`, `ReceiveFrom`, their `Try*` variants, and `IOCctl` unavailable or renamed for existing consumers. Revert the unrelated upgrade or handle it as an intentional breaking release.
   Reference: `Directory.Packages.props:8`

## Low Priority

9. [ ] Validate byte-sized route values.
   Prevent public enum values from silently truncating when serialized to byte-sized kernel fields. Validate ranges or use byte-backed enums.
   Reference: `LibNlCore/Route/RouteNetlinkSocket.cs:386-388`

## Verification Baseline

- `dotnet build -c Release` passes.
- Package creation passes.
- `git diff --check master...HEAD` passes.
- All 25 privileged tests pass.
- Existing route tests cover only simple single-path, same-family routes and do not exercise the cases above.
