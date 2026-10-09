using Catalogo.Api.Data;
using Catalogo.Api.Hubs;
using Catalogo.Api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var items = app.MapGroup("/api/items").WithTags("Items");

        items.MapGet("/", ListItems)
            .WithName("ListItems")
            .WithSummary("Search and filter catalog items")
            .WithDescription("`search` matches name, description, team, contact and tags (case-insensitive).");

        items.MapGet("/{id:int}", GetItem).WithName("GetItem").WithSummary("Get one item");
        items.MapPost("/", CreateItem).WithName("CreateItem").WithSummary("Insert a new item");
        items.MapPut("/{id:int}", UpdateItem).WithName("UpdateItem").WithSummary("Update an item (replaces every field)");
        items.MapDelete("/{id:int}", DeleteItem).WithName("DeleteItem").WithSummary("Delete an item");

        app.MapGet("/api/report", GetReport)
            .WithTags("Report")
            .WithName("GetReport")
            .WithSummary("Current status of the catalog: totals by status, type and team");

        return app;
    }

    static async Task<Ok<List<CatalogItemDto>>> ListItems(
        CatalogDbContext db, string? search, ItemType? type, ItemStatus? status, string? ownerTeam)
    {
        var query = db.Items.AsNoTracking();
        if (type is not null) query = query.Where(i => i.Type == type);
        if (status is not null) query = query.Where(i => i.Status == status);
        if (!string.IsNullOrWhiteSpace(ownerTeam)) query = query.Where(i => i.OwnerTeam == ownerTeam);

        var list = await query.OrderBy(i => i.Name).ToListAsync();

        // Free-text search runs in memory: simple, includes tags, and fine for a few thousand rows.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            list = list.Where(i =>
                i.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.OwnerTeam.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Contact.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.Tags.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        return TypedResults.Ok(list.Select(i => i.ToDto()).ToList());
    }

    static async Task<Results<Ok<CatalogItemDto>, NotFound>> GetItem(int id, CatalogDbContext db)
    {
        var item = await db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item.ToDto());
    }

    static async Task<Results<Created<CatalogItemDto>, ValidationProblem>> CreateItem(
        CatalogItemInput input, CatalogDbContext db, IHubContext<CatalogHub> hub)
    {
        if (input.Validate() is { } errors) return TypedResults.ValidationProblem(errors);

        var now = DateTime.UtcNow;
        var item = new CatalogItem { CreatedAt = now, UpdatedAt = now };
        input.ApplyTo(item);
        db.Items.Add(item);
        await db.SaveChangesAsync();

        var dto = item.ToDto();
        await hub.Clients.All.SendAsync(CatalogHub.ItemChangedEvent, new ItemChange("created", dto.Id, dto));
        return TypedResults.Created($"/api/items/{dto.Id}", dto);
    }

    static async Task<Results<Ok<CatalogItemDto>, NotFound, ValidationProblem>> UpdateItem(
        int id, CatalogItemInput input, CatalogDbContext db, IHubContext<CatalogHub> hub)
    {
        var item = await db.Items.FindAsync(id);
        if (item is null) return TypedResults.NotFound();
        if (input.Validate() is { } errors) return TypedResults.ValidationProblem(errors);

        input.ApplyTo(item);
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var dto = item.ToDto();
        await hub.Clients.All.SendAsync(CatalogHub.ItemChangedEvent, new ItemChange("updated", dto.Id, dto));
        return TypedResults.Ok(dto);
    }

    static async Task<Results<NoContent, NotFound>> DeleteItem(int id, CatalogDbContext db, IHubContext<CatalogHub> hub)
    {
        var item = await db.Items.FindAsync(id);
        if (item is null) return TypedResults.NotFound();

        db.Items.Remove(item);
        await db.SaveChangesAsync();

        await hub.Clients.All.SendAsync(CatalogHub.ItemChangedEvent, new ItemChange("deleted", id, null));
        return TypedResults.NoContent();
    }

    static async Task<Ok<CatalogReport>> GetReport(CatalogDbContext db)
    {
        var all = await db.Items.AsNoTracking().ToListAsync();

        // Every enum value is listed, even with a zero count, so charts keep a stable shape.
        var byStatus = Enum.GetValues<ItemStatus>()
            .Select(s => new CountBy(s.ToString(), all.Count(i => i.Status == s))).ToList();
        var byType = Enum.GetValues<ItemType>()
            .Select(t => new CountBy(t.ToString(), all.Count(i => i.Type == t))).ToList();
        var byTeam = all.GroupBy(i => i.OwnerTeam)
            .Select(g => new CountBy(g.Key, g.Count()))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Key).ToList();
        var recent = all.OrderByDescending(i => i.UpdatedAt).Take(5).Select(i => i.ToDto()).ToList();

        return TypedResults.Ok(new CatalogReport(
            all.Count, byStatus, byType, byTeam, recent,
            all.Count == 0 ? null : all.Max(i => i.UpdatedAt),
            DateTime.UtcNow));
    }
}
