namespace Catalogo.Api.Models;

public enum ItemType { Feature, Service, Tool }

public enum ItemStatus { Active, InDevelopment, Deprecated }

/// <summary>A feature, service or tool the team already built.</summary>
public class CatalogItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ItemType Type { get; set; }
    public ItemStatus Status { get; set; }
    public string Description { get; set; } = "";
    public string OwnerTeam { get; set; } = "";
    public string Contact { get; set; } = "";
    public string? RepoUrl { get; set; }
    public string? DocsUrl { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
