using System.ComponentModel.DataAnnotations;

namespace Catalogo.Api.Models;

/// <summary>Body for POST and PUT. PUT replaces the whole record.</summary>
public class CatalogItemInput
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required]
    public ItemType? Type { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.Active;

    [Required, StringLength(1000, MinimumLength = 5)]
    public string Description { get; set; } = "";

    [Required, StringLength(120, MinimumLength = 2)]
    public string OwnerTeam { get; set; } = "";

    [Required, StringLength(120, MinimumLength = 2)]
    public string Contact { get; set; } = "";

    [Url, StringLength(500)]
    public string? RepoUrl { get; set; }

    [Url, StringLength(500)]
    public string? DocsUrl { get; set; }

    public List<string>? Tags { get; set; }
}

public record CatalogItemDto(
    int Id,
    string Name,
    ItemType Type,
    ItemStatus Status,
    string Description,
    string OwnerTeam,
    string Contact,
    string? RepoUrl,
    string? DocsUrl,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CountBy(string Key, int Count);

public record CatalogReport(
    int Total,
    IReadOnlyList<CountBy> ByStatus,
    IReadOnlyList<CountBy> ByType,
    IReadOnlyList<CountBy> ByTeam,
    IReadOnlyList<CatalogItemDto> RecentlyUpdated,
    DateTime? LastUpdatedAt,
    DateTime GeneratedAt);

/// <summary>Pushed to every SignalR client on the "itemChanged" event.</summary>
public record ItemChange(string Action, int Id, CatalogItemDto? Item);

public static class CatalogMapping
{
    public static CatalogItemDto ToDto(this CatalogItem i) => new(
        i.Id, i.Name, i.Type, i.Status, i.Description, i.OwnerTeam, i.Contact,
        i.RepoUrl, i.DocsUrl, i.Tags, i.CreatedAt, i.UpdatedAt);

    /// <summary>Copies every input field onto the entity (used by create and update).</summary>
    public static void ApplyTo(this CatalogItemInput input, CatalogItem item)
    {
        item.Name = input.Name.Trim();
        item.Type = input.Type!.Value;
        item.Status = input.Status;
        item.Description = input.Description.Trim();
        item.OwnerTeam = input.OwnerTeam.Trim();
        item.Contact = input.Contact.Trim();
        item.RepoUrl = string.IsNullOrWhiteSpace(input.RepoUrl) ? null : input.RepoUrl.Trim();
        item.DocsUrl = string.IsNullOrWhiteSpace(input.DocsUrl) ? null : input.DocsUrl.Trim();
        item.Tags = (input.Tags ?? new List<string>())
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .OrderBy(t => t)
            .ToList();
    }

    /// <summary>Data-annotation validation plus the tag rules. Returns null when valid.</summary>
    public static Dictionary<string, string[]>? Validate(this CatalogItemInput input)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);

        var errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty("").Select(m => (Member: m, r.ErrorMessage)))
            .GroupBy(x => x.Member)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Invalid value").ToArray());

        var tags = input.Tags ?? new List<string>();
        if (tags.Count > 10)
            errors["Tags"] = new[] { "At most 10 tags." };
        else if (tags.Any(t => t is null || t.Trim().Length > 30))
            errors["Tags"] = new[] { "Each tag must be text of at most 30 characters." };

        return errors.Count == 0 ? null : errors;
    }
}
