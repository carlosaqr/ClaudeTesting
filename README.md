# Catálogo interno

This catalog lists the features, services and tools the team has already built. It has a .NET 8 API with full CRUD and Swagger, and a Vite + React dashboard that updates in real time over SignalR.

```
web (Vite + React) ──HTTP /api──▶ Catalogo.Api (ASP.NET Core 8) ──EF Core──▶ SQLite | SQL Server | PostgreSQL
        ▲                                │
        └──────── SignalR /hubs/catalog ◀┘  (pushes every insert / update / delete)
```

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)

## Quick start

```bash
# 1) API: creates catalogo.db and loads seed.json on first run
dotnet run --project api/Catalogo.Api
#    → Swagger UI: http://localhost:5180/swagger

# 2) Web app (second terminal)
cd web
npm install
npm run dev
#    → http://localhost:5173
```

Open the web app in two browser windows and edit a record in one. The other updates instantly.

## Connect your own database

Edit **`api/Catalogo.Api/database.json`**:

```jsonc
{
  "Database": {
    "Provider": "SqlServer",            // Sqlite | SqlServer | Postgres
    "ConnectionString": "Server=...;Database=Catalogo;User Id=...;Password=...;TrustServerCertificate=True",
    "CreateIfMissing": true,             // creates DB + table if the database doesn't exist
    "Seed": { "Enabled": true, "File": "seed.json" }  // only loads into an empty table
  }
}
```

- **Passwords:** don't commit them. Copy `database.Local.example.json` to `database.Local.json`, which is gitignored and overrides `database.json`. You can also set the env vars `Database__Provider` and `Database__ConnectionString`.
- **Existing database:** run the matching script in [`database/`](database/) to create the `Items` table, then start the API. Rows already in the table are used as they are. The seed only runs when the table is empty.

## API

Everything is documented and runnable at **`/swagger`**. `Catalogo.Api.http` has ready-made requests for VS Code or Visual Studio.

| Method | Path | Description |
| --- | --- | --- |
| GET | `/api/items?search=&type=&status=&ownerTeam=` | Search and filter |
| GET | `/api/items/{id}` | Get one record |
| POST | `/api/items` | Insert |
| PUT | `/api/items/{id}` | Update (replaces all fields) |
| DELETE | `/api/items/{id}` | Delete |
| GET | `/api/report` | Totals by status, type and team, plus the latest changes |
| WS | `/hubs/catalog` | SignalR: `itemChanged` event `{ action, id, item }` |

Allowed values: `type` is `Feature`, `Service` or `Tool`. `status` is `Active`, `InDevelopment` or `Deprecated`.

## Tests

```bash
dotnet test             # API integration tests
cd web && npm run build # type-check and build the web app
```

GitHub Actions runs both on every push and pull request.

## Project layout

```
api/Catalogo.Api/          ASP.NET Core API (Program.cs, Endpoints/, Data/, Models/, Hubs/)
  database.json            ← database provider + connection string
  seed.json                ← initial test data
api/Catalogo.Api.Tests/    xUnit integration tests
database/                  CREATE TABLE scripts per provider
web/                       Vite + React + TypeScript dashboard
CLAUDE.md                  Conventions for working on this repo with Claude
```
