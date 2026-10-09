using System.Text.Json.Serialization;
using Catalogo.Api.Data;
using Catalogo.Api.Endpoints;
using Catalogo.Api.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database settings live in database.json; database.Local.json (gitignored) overrides it for real credentials.
// Environment variables and command-line args are re-added so they still win (Database__ConnectionString=...).
builder.Configuration
    .AddJsonFile("database.json", optional: true, reloadOnChange: false)
    .AddJsonFile("database.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Read lazily (inside the lambda) so tests and environment overrides apply.
builder.Services.AddDbContext<CatalogDbContext>((sp, options) =>
{
    var db = DatabaseOptions.From(sp.GetRequiredService<IConfiguration>());
    _ = db.Provider.Trim().ToLowerInvariant() switch
    {
        "sqlite" => options.UseSqlite(db.ConnectionString),
        "sqlserver" or "mssql" => options.UseSqlServer(db.ConnectionString),
        "postgres" or "postgresql" or "npgsql" => options.UseNpgsql(db.ConnectionString),
        _ => throw new InvalidOperationException(
            $"Unknown Database:Provider '{db.Provider}' in database.json. Use Sqlite, SqlServer or Postgres."),
    };
});

// Enums travel as strings ("Tool", "Active") in HTTP, Swagger and SignalR.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR().AddJsonProtocol(o =>
    o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger UI publishes every endpoint as an interactive web page at /swagger.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new()
{
    Title = "Catálogo interno API",
    Version = "v1",
    Description = "Insert, update, delete and search the features, services and tools the team already built.",
}));

// The React app runs on another origin during development. SignalR needs AllowCredentials.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var options = DatabaseOptions.From(app.Configuration);
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    app.Logger.LogInformation("Database provider: {Provider}", options.Provider);
    try
    {
        // Creates database + table only when the database does not exist yet.
        if (options.CreateIfMissing) await db.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(db, options, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex,
            "Could not use the database. Check Provider and ConnectionString in database.json / database.Local.json. " +
            "If the database already existed, create the Items table with the script in /database for your provider.");
        throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.DocumentTitle = "Catálogo interno API");
app.UseCors();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new { ok = true })).WithTags("Health");
app.MapItemEndpoints();
app.MapHub<CatalogHub>(CatalogHub.Path);

app.Run();

// Lets the test project start the app with WebApplicationFactory<Program>.
public partial class Program { }
