using Catalogo.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalogo.Api.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<CatalogItem> Items => Set<CatalogItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var item = modelBuilder.Entity<CatalogItem>();
        item.ToTable("Items");
        item.HasKey(i => i.Id);
        item.Property(i => i.Name).HasMaxLength(120).IsRequired();
        item.Property(i => i.Type).HasConversion<string>().HasMaxLength(20);
        item.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        item.Property(i => i.Description).HasMaxLength(1000).IsRequired();
        item.Property(i => i.OwnerTeam).HasMaxLength(120).IsRequired();
        item.Property(i => i.Contact).HasMaxLength(120).IsRequired();
        item.Property(i => i.RepoUrl).HasMaxLength(500);
        item.Property(i => i.DocsUrl).HasMaxLength(500);
        // Tags is a primitive collection: EF Core 8 stores it as a JSON array column.
        item.HasIndex(i => i.Name);
        item.HasIndex(i => i.OwnerTeam);
    }
}
