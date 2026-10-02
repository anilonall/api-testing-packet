# Development guide

Use .NET 10 and Node 24. Start PostgreSQL, set the connection string and signing key through environment variables, apply migrations, then run API and frontend as documented in the README.

Backend dependency direction is API -> Service -> Repository -> Core, with API also referencing Repository for startup registration. Put transport concerns in controllers, workflows in services, persistence in repositories, and contracts/entities/interfaces in Core. The shared execution engine lives in Service and is used by both API workflows and CLI. Add ownership conditions to every repository query.

Create and apply migrations from the Repository project with the API as startup host:

```bash
dotnet ef migrations add <Name> --project backend/src/ApiTestingPacket.Repository --startup-project backend/src/ApiTestingPacket.API
dotnet ef database update --project backend/src/ApiTestingPacket.Repository --startup-project backend/src/ApiTestingPacket.API
```

The Vite dev server proxies `/api` to `http://localhost:5115`; keep the API launch profile and proxy synchronized if either port changes.

Frontend server data uses TanStack Query; UI-only state uses Zustand. Forms use React Hook Form and Zod. Run all commands in the README before opening a pull request.
