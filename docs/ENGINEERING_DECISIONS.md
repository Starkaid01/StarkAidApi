# StarkAid Engineering Decisions

This file exists to make the repository readable as what it is: a product codebase with explicit tradeoffs, not an accidental pile of framework defaults.

## Why JWT

`StarkAid` has more than one client surface:

- `Blazor WebAssembly`
- Android
- desktop
- realtime device-related communication

Using `JWT` reduces coupling to server-side session state and keeps auth transport consistent across these clients.

## Why a runtime Api-Key in addition to JWT

The product does not only authenticate users; it also coordinates device and command flows after login.

The backend issues a runtime `Api-Key` that becomes part of authenticated command and device traffic. That gives the system a second boundary for operational requests without turning every client into a long-lived credential holder at build time.

## Why SignalR

Support and automation are not purely request-response problems.

The platform needed:

- feedback after remote commands
- support and operator communication
- synchronized client updates
- fast state propagation without heavy polling

`SignalR` fits that shape better than rebuilding the same pattern manually over repeated HTTP calls.

## Why multiple device protocols

The product problem is heterogeneous home automation.

That means the backend had to coexist with:

- `ESP32` and UDP style device flows
- `MQTT`
- `eWeLink`
- `Tuya / Thingclips`

The architecture favors one central backend boundary over pretending one device protocol can solve every integration case cleanly.

## Why EF Core + SQL Server

The same backend owns:

- authentication
- users and plans
- devices
- schedules and routines
- support state
- telemetry and usage

That makes relational persistence a natural fit. `EF Core` + `SQL Server` keeps the transactional model straightforward while still allowing product features to evolve inside one application boundary.

## Why hosted services

This system has product-native background work:

- scheduled actions
- reminder processing
- periodic resets
- subscription checks

Those concerns are part of the product runtime, so they live in hosted services instead of being treated as documentation-only future work.

## Why the public repo looks newer than the product

The public repository is a publishable snapshot of a longer-lived private codebase.

Recent public commits mostly show:

- configuration externalization
- documentation cleanup
- credential sanitization
- CI visibility
- reviewability improvements

That is expected. The repo is being shaped to prove real engineering work without leaking private provider credentials or broken local assumptions.
