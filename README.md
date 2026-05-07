# Umojo Parking ArcGIS Pro Add-in — Proof of Concept

## What this is

A working proof of concept demonstrating the Umojo Parking add-in workflow inside ArcGIS Pro. All data and the API client are mocked. Auth is simulated. No production concerns are implemented — code quality, tests, security hardening, and CI/CD are deliberately deferred.

## How to run

1. Install ArcGIS Pro 3.x (trial is fine — sign up at [esri.com/trial](https://www.esri.com/en-us/arcgis/products/arcgis-pro/buy/free-trial)).
2. Install Visual Studio 2022 with the **.NET desktop development** workload and the **ArcGIS Pro SDK for .NET** extension.
3. Open `UmojoParkingPoC.sln`, F5.
4. In Pro, click the **Umojo Parking** ribbon tab → **Sign In** (any credentials) → **Asset Manager**.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│  DockPane / ViewModels / MapTools / SignInWindow    │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
              ┌─────────────────┐
              │ IUmojoApiClient │   ◄── single integration seam
              └────────┬────────┘
                       │
                       ▼
              ┌──────────────────────┐
              │ MockUmojoApiClient   │
              │  - simulated OAuth   │
              │  - in-memory assets  │
              │  - simulated delays  │
              │  - one validation    │
              └──────────────────────┘
```

When the real engagement starts, `MockUmojoApiClient` is replaced by a real implementation against the Umojo REST API. **Everything UI-side stays unchanged.** That seam is the architectural point of this PoC.

## What's mocked

- **OAuth sign-in** — any credentials are accepted; returns a fake bearer GUID after a 1.5s delay.
- **Asset data** — 10 hardcoded parking assets (5 zones, 5 meters) seeded around downtown Chicago in WGS84.
- **API latency** — 1.2–1.5s simulated delays on every call.
- **Validation** — one rule: `HourlyRate <= 20` (returns `Hourly rate exceeds municipal cap of $20.00`).

## Production considerations (not implemented in this PoC)

- **Authentication.** Replace the mock client with a real OAuth client. Recommended library: `IdentityModel.OidcClient` or `Microsoft.Identity.Client`. Token storage in Windows Credential Manager (`CredentialManagement` package or P/Invoke `wincred.h`). Refresh-token rotation handled inside the API client.
- **Real Umojo API integration.** Drop in a `UmojoApiClient` implementing `IUmojoApiClient` against the actual REST endpoints. Add typed request/response models, retry policy via Polly, structured error handling, and per-endpoint timeouts.
- **ArcGIS Enterprise connection.** Load real feature services from Umojo's hosted Enterprise instead of in-memory graphics. Handle Portal sign-in, layer permissions, edit-tracking, and a defined refresh strategy.
- **Tests.** xUnit project for ViewModels and the API client. Pro SDK has limited but useful integration testing affordances via the ArcGIS.Pro.Test framework.
- **CI/CD.** GitHub Actions pipeline to build the solution, code-sign the `.esriAddinX`, and publish to an internal distribution channel (network share, Pro portal, or per-city deployment).
- **Mock services for development.** When devs lack access to the Umojo staging API, a containerized mock backend (Docker + ASP.NET Core minimal API) is a useful pattern. Same `IUmojoApiClient` interface, different implementation.
- **Logging and telemetry.** Serilog for structured logs (sink to a city-managed log aggregator). OpenTelemetry for tracing API calls, with correlation IDs propagated from the add-in to the backend.
- **Distribution.** Code-signed `.esriAddinX` deployed to each city's add-in folder, or via auto-update through Esri's add-in manager (`AddInFolders` registry key + a hosted `addins.xml` manifest).
- **Geometry editing.** "Move on Map" / shape-edit tools are a natural follow-up; the Pro SDK provides a `SketchTool` workflow that fits the existing architecture.

## License / contact

TBD
