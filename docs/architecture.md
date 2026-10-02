# Architecture and decisions

API Testing Packet is a layered modular monolith. The ASP.NET Core API is the only server deployment unit, while the CLI references the same service-layer execution engine rather than recreating request behavior.

## Dependency direction

```text
API --------> Service --------> Repository --------> Core
 |                                                     ^
 +-----------------------> Repository -----------------+

CLI --------> Service -------> Core
```

- `ApiTestingPacket.Core` contains entities, DTOs, application exceptions, execution contracts, and repository/service interfaces. It has no infrastructure dependency.
- `ApiTestingPacket.Repository` contains `ApiTestingPacketDbContext`, EF configurations, migrations, and repository implementations.
- `ApiTestingPacket.Service` contains business workflows, validation, auth/token behavior, import/export, and the shared HTTP execution engine. It consumes repository abstractions from Core and references Repository as the traditional N-layer persistence dependency.
- `ApiTestingPacket.API` contains controllers, authentication/HTTP configuration, exception-to-Problem-Details mapping, and composition only.
- `ApiTestingPacket.Cli` directly uses the service execution engine.

Controllers never query EF Core directly. User ownership is enforced by repository query boundaries and user IDs are supplied by the authenticated API adapter.

## Decisions

- **Storage:** ownership, ordering, searchable metadata, and relationships are relational. Flexible headers, parameters, bodies, auth settings, assertions, and extraction rules use PostgreSQL JSONB to prevent a brittle table-per-property model.
- **Execution snapshots:** history stores a sanitized request name, method, URL, response metadata, and assertion outcomes so it remains understandable after source requests change. Authorization, cookie, API-key, and configured secret values are never copied into snapshots.
- **Variables:** runtime/extracted variables override the selected environment; environment values override packet defaults. Missing variables fail execution instead of being sent literally.
- **SSRF policy:** only HTTP(S) is accepted. Private, loopback, link-local, and unspecified addresses are denied by default in production, including all resolved DNS addresses. Local development can opt in. Redirects are handled manually and revalidated.
- **CLI reuse:** file packets are mapped to the same execution contracts, interpolation, assertion, extraction, redaction, and result models used by the web application.
- **Imports:** complete parsing and validation happens before persistence, then the new aggregate is saved transactionally. Unsupported OpenAPI constructs produce warnings.
