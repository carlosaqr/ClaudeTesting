using System.Text.Json;
using System.Text.Json.Serialization;
using Catalogo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Data;

/// <summary>Loads seed.json into an empty Items table on startup.</summary>
public static class SeedData
{
    public static async Task SeedAsync(CatalogDbContext db, DatabaseOptions options, ILogger logger)
    {
        // Only an empty table is seeded, so existing data in your database is never touched.
        if (!options.Seed.Enabled || await db.Items.AnyAsync())
            return;

        var file = options.Seed.File;
        var path = Path.IsPathRooted(file) ? file : Path.Combine(AppContext.BaseDirectory, file);
        if (!File.Exists(path))
        {
            logger.LogWarning("Seed file {Path} not found; starting with an empty catalog.", path);
            return;
        }

        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        json.Converters.Add(new JsonStringEnumConverter());
        var inputs = JsonSerializer.Deserialize<List<CatalogItemInput>>(await File.ReadAllTextAsync(path), json) ?? new();

        var now = DateTime.UtcNow;
        var added = 0;
        foreach (var input in inputs)
        {
            var errors = input.Validate();
            if (errors is not null)
            {
                logger.LogWarning("Skipping seed entry '{Name}': {Errors}", input.Name,
                    string.Join("; ", errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}")));
                continue;
            }
            var item = new CatalogItem { CreatedAt = now, UpdatedAt = now };
            input.ApplyTo(item);
            db.Items.Add(item);
            added++;
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} catalog items from {Path}.", added, path);
    }
}
