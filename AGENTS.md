# AGENTS.md

This file provides guidance to AI coding agents when working with code in this repository.

## Project Overview

LibNlCore is a high-performance .NET wrapper for Linux netlink sockets. It provides type-safe, zero-allocation abstractions over the kernel's netlink protocol for network interface management, ethtool features, and network namespace operations. Linux-only (`[SupportedOSPlatform("linux")]`).

## Build and Test Commands

```bash
dotnet build                # Build entire solution

# Tests must run as root (they create virtual network interfaces)
dotnet build && sudo -E --preserve-env=PATH bash -c "dotnet test --no-build"

# Run a single test
dotnet build && sudo -E --preserve-env=PATH bash -c "dotnet test --no-build --filter 'FullyQualifiedName~ClassName.MethodName'"
```

Tests are globally marked `[DoNotParallelize]` and run sequentially.

## Build Configuration

- **Target**: net10.0, C# 14
- **Strict mode**: `TreatWarningsAsErrors`, `AnalysisMode=Recommended`, `EnforceCodeStyleInBuild`, `Nullable=enable`
- **Unsafe code**: Enabled (required for ref struct protocol handling)
- Central package versioning via `Directory.Packages.props`

## Architecture

Layered protocol wrapper with two socket families:

```
LinkCollection / Link / LinkFeatures         ← High-level Links API (user-facing)
        ↓
RouteNetlinkSocket / EthToolNetlinkSocket    ← Socket-level API
        ↓                    ↓
        ↓         GenericNetlinkSocket<TCmd>  ← Generic family base (templated on command enum)
        ↓                    ↓
   NetlinkSocket (abstract base)             ← Socket lifecycle, send/receive, Get<>/Post<>
        ↓
   Protocol Layer                            ← Message/attribute builders
   (NetlinkMessageWriter, NetlinkAttributeWriter, SpanReader/SpanWriter)
        ↓
   LinuxSocketBase (from LinuxCore)          ← Raw syscalls
```

**Key directories:**
- `LibNlCore/Links/` — High-level Links API: `LinkCollection` (entry point, manages socket lifecycle), `Link` (mutable network interface facade with settable properties like `Up`, `MacAddress`, `Master`), `LinkAddressCollection` (IP address management per link), `LinkFeatures` (ethtool feature toggling)
- `LibNlCore/Route/` — NETLINK_ROUTE: socket-level link/address management, `LinkInformation` record, `LinkAddress`
- `LibNlCore/Generic/` — NETLINK_GENERIC: generic netlink base + ethtool
- `LibNlCore/Protocol/` — Zero-allocation message/attribute parsing and writing (ref structs), plus kernel constant enums/structs (C-style naming, relaxed rules in .editorconfig)

**Core patterns:**
- **Ref struct builders**: `NetlinkMessageWriter<THeader, TAttr>` and `NetlinkAttributeWriter<TAttr>` are stack-only, generic over `unmanaged` header structs and `Enum` attribute types
- **Lazy collections**: `NetlinkMessageCollection` and `NetlinkAttributeCollection` implement iterator protocol for zero-copy parsing
- **Pooled buffers**: `NetlinkBuffer` wraps `ArrayPool<byte>` for send/receive buffers
- **Family resolution**: `GenericNetlinkFamilyResolver` caches generic netlink family IDs (thread-safe via `Lock`)
- **Nested attributes**: Scoped `WriteNested<>()` for complex hierarchical request/response structures

## Testing Patterns

- Tests follow create-test-delete with try/finally for resource cleanup (virtual interfaces, bridges)
- Socket lifecycle managed via `using` statements
- `InternalsVisibleTo("LibNlCore.Tests")` exposes internals to the test project

## Netlink Validation Policy

- Do not duplicate semantic or range validation already performed by the Linux kernel for outbound netlink requests. Let the request reach the kernel so callers retain its errno and extended-ack details through `NetlinkException`.
- Validate locally when a value cannot be represented safely on the wire, when enforcing a library-level invariant the kernel cannot know, or when Linux accepts a replace/delete value but redirects it to a different route key or turns an explicit key component into a wildcard.
- Allow harmless normalization of stored values. Also allow attributes the kernel ignores during deletion because they are not part of the deletion key.
- Verify new validation against kernel source and, where practical, a raw-netlink test. Linux behavior can be asymmetric; for example, route output interface index zero is treated as unspecified while a negative index is rejected.
- Put checks for dangerous mutation-key reinterpretation at the serialization boundary and explain the kernel behavior in the exception or a nearby comment. Preserve established public validation contracts unless intentionally making a breaking change.
- Trust incoming kernel netlink messages rather than adding semantic or exact-length validation to response parsers. Bounds-checked parsing may still throw on malformed data; local validation is for outbound requests.

## Code Style

Configured in `.editorconfig`:
- 4-space indent for C#, 2-space for XML/csproj
- LF line endings, no final newline
- File-scoped namespaces
- `var` usage allowed (IDE0008 suppressed)
- Expression-bodied members allowed (IDE0021/0022/0023 suppressed)
- Kernel constant files in `Protocol/`
- Test files allow underscores in names (CA1707 suppressed)

## CI Pipeline

GitHub Actions workflow (`.github/workflows/pipeline.yml`):
- **Validate**: Builds and tests on `ubuntu-latest` and `ubuntu-24.04-arm` matrix, runs on push/PR to `master`
- **Publish**: Packs and pushes to NuGet when `PUBLISH` repo variable is `true`, or when `PUBLISH` is `auto` and on `master` branch

## Dependencies

- **LinuxCore**: P/Invoke bindings to Linux syscalls (socket, bind, sendmsg, etc.)
- **NetNsCore**: Network namespace operations
- **NetworkingPrimitivesCore**: `MACAddress` and IP utilities
- **MSTest**: Testing framework (test project only)
