# Claude Code Instructions

## Project Overview

This is a .NET library (NuGet packages) extending Azure Durable Task Framework.
Multi-target: net10.0, net9.0, net8.0. Frontend: React 18 + MUI 5 + TypeScript + Vite.

## Development Workflow

Before every commit, follow this sequence:

### 1. Build

```bash
dotnet build
```

Must pass with **0 warnings and 0 errors**.

### 2. Unit Tests

```bash
dotnet test --filter "FullyQualifiedName~InMemory" --no-build
```

InMemory tests run without Docker. All 3 TFMs (net10.0, net9.0, net8.0) must pass.

### 3. UI Smoke Test via Aspire

Start all services at once using the Aspire AppHost:

```bash
dotnet run --project samples/AppHost/AppHost.csproj
```

This starts: Server, Api, Ui, OrchestrationWorker, CarWorker, FlightWorker, HotelWorker, BpmnWorker.

Then use Playwright to verify the UI:
1. Navigate to the UI (https://localhost:5002)
2. Verify the home page loads ("Welcome to Durable Task UI")
3. Navigate to Orchestrations page - verify table renders
4. Create a **BookParallel** orchestration (Name: `BookParallel`, Version: `v1`, Input: `{}`)
5. Wait a few seconds for workers to process it
6. Refresh and verify Status changes to **Completed**
7. Check the History tab - verify the full execution flow is visible
8. Take screenshots as evidence

After verification, stop the Aspire process.

### 4. Commit

Only commit after all steps above pass successfully.

## Package Versioning Rules

Since src/ projects are **published NuGet libraries**:

- **Microsoft.Extensions.\*** and **Microsoft.EntityFrameworkCore.\*** must stay at the **minimum
  compatible version** for each TFM (e.g., 10.0.0 not 10.0.3 for net10.0). This maximizes
  compatibility for consumers.
- **Non-framework packages** (DurableTask.Core, gRPC, Protobuf, SourceLink, etc.) should be
  updated to latest stable versions.
- **Test, sample, and benchmark projects** (not published) can use latest versions freely.

## Key Commands

```bash
# Full test suite (requires Docker for MySQL, PostgreSQL, SQL Server, Azurite)
docker compose up -d
dotnet test

# Just InMemory tests (no Docker needed)
dotnet test --filter "FullyQualifiedName~InMemory"

# UI frontend (in src/LLL.DurableTask.Ui/app/)
pnpm install
pnpm build

# Run all samples via Aspire
dotnet run --project samples/AppHost/AppHost.csproj
```

## CI/CD

- GitHub Actions: `.github/workflows/build.yml`
- Triggers on push to `main` branch and `v*` tags
- Release: tag with `v*` to publish to NuGet
