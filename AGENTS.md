# API Testing Packet Engineering Rules

## Product and architecture

- Build a production-quality, open-source V1 as a modular monolith.
- Keep one conceptual execution engine for interactive sends, scenarios, and CLI runs.
- Preserve the backend dependency direction API -> Service -> Core and API -> Repository -> Core; controllers stay thin and never query EF Core directly.
- Keep the shared execution engine in Service so API workflows and CLI use one implementation.
- Every persisted workspace query must enforce the authenticated user's ownership.
- Preserve useful execution history through sanitized immutable snapshots.

## Security

- Never commit, log, export, or persist plaintext credentials, bearer tokens, refresh tokens, or secret environment values.
- Only outbound `http` and `https` URLs are valid. Production denies loopback, link-local, and private-network destinations by default; development access is explicitly configurable.
- Redact sensitive headers and query values before logging, persistence, or report generation.
- Validate untrusted imports completely before starting a database transaction.
- Return problem details without stack traces or database internals.

## Backend

- Target .NET 10 with nullable reference types and warnings treated as errors.
- Propagate `CancellationToken`, use async I/O, `IHttpClientFactory`, dependency injection, and structured logging.
- Use PostgreSQL through EF Core migrations; never substitute `EnsureCreated` in application startup.
- Prefer relational columns for identity, ownership, ordering, and querying; use JSONB for flexible request/test configuration.
- Add tests for execution behavior, security boundaries, ownership, and import validation.

## Frontend

- Keep TypeScript strict and do not use `any`.
- TanStack Query owns server state; Zustand owns transient UI state; React Hook Form and Zod own form validation.
- Use semantic controls, labels, visible focus states, keyboard access, responsive layouts, and polished dark/light themes.
- Keep unsaved request state while switching editor tabs and make failures understandable.

## Quality gates

- Before handoff, run backend restore/build/test/format checks; frontend lint/typecheck/test/build; and `docker compose config` when available.
- Keep documentation aligned with implemented behavior and record meaningful tradeoffs in `docs/architecture.md`.
- Do not leave TODO, FIXME, HACK, fake metrics, sample secrets, or claims that were not verified.
