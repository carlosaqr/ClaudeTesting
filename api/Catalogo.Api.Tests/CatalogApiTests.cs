using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Catalogo.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Catalogo.Api.Tests;

/// <summary>Starts the real API on a throwaway SQLite file, with seeding off.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"catalogo-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Added last, so it overrides database.json and database.Local.json.
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite",
            ["Database:ConnectionString"] = $"Data Source={_dbPath}",
            ["Database:CreateIfMissing"] = "true",
            ["Database:Seed:Enabled"] = "false",
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch (IOException) { }
    }
}

public class CatalogApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client = factory.CreateClient();

    private static CatalogItemInput NewInput(string name = "Exportador a Excel") => new()
    {
        Name = name,
        Type = ItemType.Tool,
        Status = ItemStatus.Active,
        Description = "Exporta reportes a xlsx",
        OwnerTeam = "Data",
        Contact = "Carlos",
        RepoUrl = "https://example.com/repo",
        Tags = new() { "Python", " excel ", "python" },
    };

    [Fact]
    public async Task Crud_roundtrip_works()
    {
        var create = await _client.PostAsJsonAsync("/api/items", NewInput("Crud item"), Json);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CatalogItemDto>(Json);
        Assert.NotNull(created);
        Assert.Equal(new List<string> { "excel", "python" }, created!.Tags.ToList());

        var fetched = await _client.GetFromJsonAsync<CatalogItemDto>($"/api/items/{created.Id}", Json);
        Assert.Equal("Crud item", fetched!.Name);

        var change = NewInput("Crud item renamed");
        change.Status = ItemStatus.Deprecated;
        var update = await _client.PutAsJsonAsync($"/api/items/{created.Id}", change, Json);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<CatalogItemDto>(Json);
        Assert.Equal(ItemStatus.Deprecated, updated!.Status);
        Assert.True(updated.UpdatedAt >= created.UpdatedAt);

        var delete = await _client.DeleteAsync($"/api/items/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/items/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_input_returns_400()
    {
        var bad = new CatalogItemInput { Name = "x", Description = "", OwnerTeam = "", Contact = "" };
        var response = await _client.PostAsJsonAsync("/api/items", bad, Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_and_delete_missing_item_return_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.PutAsJsonAsync("/api/items/99999", NewInput(), Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync("/api/items/99999")).StatusCode);
    }

    [Fact]
    public async Task Search_and_report_reflect_data()
    {
        await _client.PostAsJsonAsync("/api/items", NewInput("Buscador de churn"), Json);

        var found = await _client.GetFromJsonAsync<List<CatalogItemDto>>("/api/items?search=CHURN", Json);
        Assert.Contains(found!, i => i.Name == "Buscador de churn");

        var tools = await _client.GetFromJsonAsync<List<CatalogItemDto>>("/api/items?type=Tool", Json);
        Assert.All(tools!, i => Assert.Equal(ItemType.Tool, i.Type));

        var report = await _client.GetFromJsonAsync<CatalogReport>("/api/report", Json);
        Assert.True(report!.Total >= 1);
        Assert.Equal(3, report.ByStatus.Count);
        Assert.Equal(report.Total, report.ByStatus.Sum(s => s.Count));
    }
}
