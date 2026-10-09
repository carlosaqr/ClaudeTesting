# CLAUDE.md

Guidance for Claude (and humans) working in this repository.

## What this is

**Catálogo interno**: a catalog of the features, services and tools the team has already built, so people search one link instead of asking in chat. Built for Flock's AI Day (Explorer challenge: "Catálogo interno con backend real").

- `api/Catalogo.Api`: ASP.NET Core 8 minimal API + EF Core. Full CRUD over one `Items` table, a status report and a SignalR hub that pushes every change. Swagger UI at `/swagger` publishes all the endpoints as a web page.
- `api/Catalogo.Api.Tests`: xUnit integration tests (`WebApplicationFactory`) on a throwaway SQLite file.
- `web/`: Vite + React + TypeScript. Live status report plus a records table with create, edit and delete. It listens to SignalR and refreshes on every change.
- `database/`: `CREATE TABLE` scripts for SQL Server, PostgreSQL and SQLite, for databases that already exist.

## Commands

```bash
# API: http://localhost:5180 (Swagger at /swagger)
dotnet run --project api/Catalogo.Api
dotnet test                                  # from the repo root

# Web: http://localhost:5173 (proxies /api and /hubs to :5180)
cd web && npm install && npm run dev
npm run build && npm run lint                # what CI runs
```

Start the API before the web app. CI (`.github/workflows/ci.yml`) builds and tests both on every push and PR.

## Database configuration

- All settings live in `api/Catalogo.Api/database.json`, section `Database`: `Provider` (`Sqlite` | `SqlServer` | `Postgres`), `ConnectionString`, `CreateIfMissing` and `Seed`.
- Real credentials go in `database.Local.json` (gitignored; copy `database.Local.example.json`) or in env vars `Database__Provider` / `Database__ConnectionString`. Load order, last wins: appsettings → database.json → database.Local.json → env vars → command line.
- `CreateIfMissing=true` runs `EnsureCreated`, which creates the database and table only when the **database** doesn't exist. For an existing database, run the script in `database/` first. There are no EF migrations yet.
- `seed.json` loads only when `Items` is empty, so existing data is never overwritten.
- Never commit a real password. `database.json` must stay safe to publish.

## Architecture and conventions

- Endpoints are in `Endpoints/ItemEndpoints.cs` and use `TypedResults`. Every write (POST/PUT/DELETE) must broadcast `ItemChange` on `CatalogHub.ItemChangedEvent` (`"itemChanged"`). The web app depends on it.
- PUT replaces the whole record: the client always sends every field.
- Validation: DataAnnotations on `CatalogItemInput` plus tag rules in `CatalogMapping.Validate()`. It returns RFC 7807 `ValidationProblem` (400). The form shows `errors[field]` next to each input.
- Enums travel as strings (`"Tool"`, `"InDevelopment"`) in HTTP, Swagger and SignalR. Keep the `JsonStringEnumConverter` registrations in `Program.cs` in sync.
- Tags are normalized: trimmed, lowercased, de-duplicated and sorted. They're stored as a primitive collection (JSON on SQLite/SQL Server, `text[]` on Postgres).
- Timestamps are UTC (`DateTime.UtcNow`). The web app appends `Z` when SQLite drops it (`web/src/format.ts`).
- Free-text `search` filters in memory after DB-side filters. That's fine for thousands of rows; move it into SQL if the table grows.
- When you change the model, update all of these together: `Models/`, `CatalogDbContext`, `database/*.sql`, `web/src/api.ts` types and the tests.
- UI text is Spanish (Argentina); code, comments and commits are English.
- Web: no UI library. Styles are in `src/index.css` with light and dark tokens. API calls go only through `src/api.ts`, and real-time only through `src/useCatalogHub.ts`.

## Project skills (`.claude/skills/`)

- `caveman`: Caveman makes AI coding agents respond in compressed caveman-style prose while preserving technical substance
- `ponytail`: You know him. Long ponytail. Oval glasses. Has been at the company longer than the version control. You show him fifty lines; he looks at them, says nothing, and replaces them with one.

Claude Code loads these automatically when a request matches their description. You can also call one directly with `/add-catalog-field`.

## Out of scope for v1 (good next steps)

Authentication and roles, EF migrations, pagination, an audit log of changes, and serving the built web app from the API (`wwwroot`) for a single deployment.
