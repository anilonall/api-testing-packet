# API Testing Packet

API Testing Packet is a self-hosted API workbench for building HTTP requests, chaining response values, checking assertions, running ordered scenarios, and executing the same versioned packets in CI. It combines a focused React developer interface with a .NET execution engine shared by the web API and CLI.

> Status: usable V1 foundation. The core execution workflow, authenticated workspaces, collections, environments, scenarios, history, native import/export, bounded OpenAPI 3 import, CLI reporting, containers, and automated tests are implemented. See [Current limitations](#current-limitations) for honest boundaries.

## Screenshots

Screenshots will be added after the first tagged release. The application currently includes a polished responsive dark/light workbench rather than placeholder mockups.

## Features

- Email/password accounts with BCrypt hashing, short-lived JWTs, HttpOnly rotating hashed refresh tokens, revocation, and rate-limited auth endpoints.
- Per-user collections, nested folders, request duplication, environments, scenarios, execution history, search, pagination, and dashboard summaries.
- HTTP methods, query parameters, headers, JSON/text/form/multipart fields, bearer/basic/API-key authentication, timeouts, and variable interpolation.
- One shared execution core for manual sends, sequential scenarios, and CLI runs.
- Status, duration, header, JSON path/value/type, array length, format, required-field, and recursive sensitive-field assertions.
- JSON/header extraction into runtime variables for subsequent steps.
- Sanitized immutable history snapshots, response size limits, scheme restrictions, and configurable private-network policy.
- Versioned `.atp.json` import/export and OpenAPI 3 JSON/YAML import with explicit warnings.
- Console, JSON, and JUnit CLI output with CI-friendly exit codes.

## Architecture

The backend is a layered modular monolith with the primary flow `API -> Service -> Repository -> Core`: `Core` owns entities, DTOs, exceptions, and interfaces; `Repository` owns EF Core and PostgreSQL; `Service` owns business logic and the shared request-execution engine; and `API` is a thin HTTP host. The CLI consumes the same `Service` execution engine. PostgreSQL stores identities and queryable ownership metadata relationally, while flexible request definitions and sanitized snapshots use JSONB.

See [architecture](docs/architecture.md) and [execution engine](docs/execution-engine.md).

## Technology

- .NET 10, ASP.NET Core, EF Core, Npgsql/PostgreSQL, JWT, BCrypt, OpenAPI
- React 19, strict TypeScript, Vite, React Router, TanStack Query, Zustand, React Hook Form, Zod, PrimeReact
- xUnit, Vitest, React Testing Library, Docker Compose, GitHub Actions

## Requirements

- Docker 29+ and Docker Compose (recommended), or
- .NET SDK 10, Node.js 24, npm 11, and PostgreSQL 16+

## Docker quick start

```bash
cp .env.example .env
# Replace POSTGRES_PASSWORD and JWT_SIGNING_KEY in .env
docker compose up --build
```

Open `http://localhost:3000`. Compose explicitly sets `ApplyMigrations=true`, waits for PostgreSQL, applies EF migrations, and then starts the API. Do not reuse the example secrets for a shared deployment.

## Manual development

Start PostgreSQL, then:

```bash
dotnet restore backend/ApiTestingPacket.slnx
dotnet ef database update --project backend/src/ApiTestingPacket.Repository --startup-project backend/src/ApiTestingPacket.API
dotnet run --project backend/src/ApiTestingPacket.API

cd frontend
npm ci
npm run dev
```

The Vite development server proxies `/api` and `/health` to the API launch URL at `http://localhost:5115`, so registration and refresh cookies remain same-origin. Set `VITE_API_URL` only when intentionally using a different API origin.

### Configuration

Environment variables use ASP.NET Core's `__` separator:

| Variable | Purpose |
|---|---|
| `ConnectionStrings__Database` | PostgreSQL connection string |
| `Jwt__SigningKey` | At least 32 bytes; use 64 random characters in deployments |
| `Jwt__Issuer`, `Jwt__Audience` | JWT validation scope |
| `OutboundRequests__AllowPrivateNetworks` | Allows loopback/private targets; `false` in production by default |
| `OutboundRequests__MaxResponseBytes` | Maximum captured response, default 5 MiB |
| `AllowedOrigins__0` | Trusted frontend origin |
| `ApplyMigrations` | Explicitly apply pending migrations at startup |
| `VITE_API_URL` | Browser API base URL; blank uses same origin |

## CLI

Run directly from source:

```bash
dotnet run --project backend/src/ApiTestingPacket.Cli -- run examples/httpbin-workflow.atp.json --environment demo
dotnet run --project backend/src/ApiTestingPacket.Cli -- run examples/httpbin-workflow.atp.json --report junit --output reports/results.xml
```

Or create and install a local tool package:

```bash
dotnet pack backend/src/ApiTestingPacket.Cli -c Release -o artifacts
dotnet tool install --global --add-source artifacts ApiTestingPacket.Cli
api-testing-packet run examples/httpbin-workflow.atp.json
```

Exit code `0` means every request/assertion passed, `1` means execution/test failure, and `2` means invalid CLI input or packet data. Private/localhost targets require the explicit `--allow-private` flag.

## Variables and assertions

Use `{{baseUrl}}/users/{{userId}}` in URLs, headers, auth values, and bodies. Precedence is packet defaults (when present), selected environment, then runtime/extracted values. A missing variable fails before sending.

Assertions are structured definitions in packets. Example:

```json
{"kind":"JsonValue","operator":"Equals","path":"user.id","expected":"42","name":"Correct user"}
```

See [assertions](docs/assertions.md) and [environments](docs/environments.md).

## Packet, scenarios, and imports

The schema is versioned with `"schemaVersion": 1`. Scenarios run in order and extracted values override environment values for later steps. Export omits secret environment values by default. See [packet format](docs/packet-format.md) and the [example packet](examples/httpbin-workflow.atp.json).

OpenAPI import accepts OpenAPI 3 JSON/YAML. It imports servers, supported HTTP operations, query/header parameters, and JSON body examples. It reports unsupported parameter/body/security constructs; imports are parsed before the collection is persisted. See [OpenAPI import](docs/openapi-import.md).

## CI/CD

This repository runs backend and frontend quality gates in `.github/workflows/ci.yml`. Another repository can run a packet with:

```yaml
- uses: actions/setup-dotnet@v5
  with:
    dotnet-version: '10.0.x'
- run: dotnet tool install --global ApiTestingPacket.Cli
- run: api-testing-packet run tests/api.atp.json --environment ci --report junit --output results.xml
```

Pin the tool version in production workflows.

## Testing

```bash
dotnet format backend/ApiTestingPacket.slnx --verify-no-changes
dotnet build backend/ApiTestingPacket.slnx --configuration Release
dotnet test backend/ApiTestingPacket.slnx --configuration Release

cd frontend
npm ci
npm run lint
npm run typecheck
npm test
npm run build
```

Repository tests do not call public APIs. The example packet uses a public demo service only for opt-in manual runs.

## Security

Production denies private, loopback, link-local, and unspecified destination addresses by default and only permits HTTP(S). DNS results are checked before connecting, redirects are disabled, responses and timeouts are bounded, and sensitive snapshots are redacted. This reduces but cannot eliminate all SSRF/DNS-rebinding risk in every network topology; deploy the backend with egress controls. The sensitive-field assertion is a helper, not a security scanner. Read [SECURITY.md](SECURITY.md) and [security design](docs/security.md).

## Repository structure

```text
backend/    Core, Repository, Service, thin ASP.NET Core API, CLI, migrations, tests
frontend/   React/Vite application and component tests
docs/       architecture and user/developer documentation
examples/   versioned test packets
.github/    CI, Dependabot, issue and pull-request templates
```

## Current limitations

- Multipart V1 supports text fields; interactive binary file streaming is not yet exposed in the React editor.
- OpenAPI import intentionally omits callbacks, links, polymorphic schema generation, and automatic OAuth flows.
- Scenario authoring is currently packet/API driven; the UI lists and runs stored definitions but does not yet provide drag-and-drop composition or an active stop control.
- Advanced request auth, assertion, and extraction authoring is currently packet/API driven; the workspace tabs preserve editor state and display results but do not yet expose every configuration as a visual form.
- Redirects are disabled rather than revalidated hop-by-hop.
- HTML reporting is not included; console, JSON, and JUnit are supported.
- Secret environment values are masked conceptually and excluded from exports, but at-rest application-level encryption is not included; secure the PostgreSQL volume and deployment.

## Contributing, roadmap, and license

Read [CONTRIBUTING.md](CONTRIBUTING.md). Near-term roadmap items are the scenario visual editor, safe streamed file upload, redirect revalidation, and richer OpenAPI examples. API Testing Packet is available under the [MIT License](LICENSE).
