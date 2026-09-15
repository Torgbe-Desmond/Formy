# FastTransfers API (C#, SQL Server)

A single-project ASP.NET Core Web API — no Clean Architecture layering. Structure
mirrors the Node/Express app directly:

| Node                     | C#                          |
|--------------------------|------------------------------|
| `models/index.js`        | `Models/Entities.cs`         |
| `config/db.js`           | `Data/AppDbContext.cs`       |
| `controllers/*.js`       | `Controllers/*.cs`           |
| `services/*.js`          | `Services/*.cs`              |
| `middleware/errorHandler.js` | `Middleware/ExceptionMiddleware.cs` + `Exceptions/AppExceptions.cs` |
| `middleware/auth.js`     | built-in ASP.NET Core JWT bearer auth (configured in `Program.cs`) |
| `app.js` + `server.js`   | `Program.cs`                 |

File content (previously a separate MongoDB collection via `fileStorage.service.js`)
now lives directly in SQL Server, in the `FileContents` table — there's no external
or blob storage provider anymore.

## Setup

1. **Install the .NET 8 SDK** if you don't have it.
2. Copy `.env.example` to `.env` and fill in a real connection string and JWT secret.
3. Restore packages and build:
   ```bash
   dotnet restore
   dotnet build
   ```
   > This repo was written without network access to nuget.org, so package
   > versions haven't been restore-verified here — if a version bump is needed
   > (e.g. `PuppeteerSharp`), `dotnet restore` will tell you.
4. Create the initial migration and apply it (make sure your SQL Server
   connection string in `.env`/`appsettings.json` is reachable first):
   ```bash
   dotnet tool install --global dotnet-ef   # if you don't have it
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   `Program.cs` also calls `db.Database.MigrateAsync()` automatically on
   startup, so once the migration exists it'll apply itself in dev.
5. Run it:
   ```bash
   dotnet run
   ```
   Swagger UI is available at `/swagger` in Development.

## PDF export

`ExportController` uses **PuppeteerSharp** (headless Chromium) as the direct
equivalent of the Node app's `puppeteer`-based `export.controller.js`. On first
run it downloads a matching Chromium build into the app's local cache — make
sure the host has outbound network access and ~200MB free disk for that, or
pre-fetch it at container build time if you'd rather not do it at first request.

## What changed vs. the old Clean Architecture version

- Domain/Application/Infrastructure/API layers, MediatR commands/queries, and
  the repository/unit-of-work abstractions are gone — everything is one
  project with `Controllers/`, `Services/`, `Models/`, `Data/`.
- `SchemaTemplate` is now multi-schema: `EntrySchema` + a `Schemas` collection
  (new `SchemaEntry` table), matching the Node app's `schemas` map +
  `entrySchema`. Legacy single-schema request bodies are still accepted and
  wrapped into a `"Main"` entry.
- New `GET /api/breadcrumb/{type}/{id}` and `POST /api/export/pdf/{id}`
  endpoints, matching the Node app's `breadcrumb.controller.js` and
  `export.controller.js`.
- File storage is SQL Server only now (`FileContents` table) — no local disk,
  blob, or Mongo storage option.

Fresh migration — this assumes no existing production data needs preserving
(per your instruction), so there's no data-migration step for old single-schema
rows.
