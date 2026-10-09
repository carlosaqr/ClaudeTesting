namespace Catalogo.Api.Data;

/// <summary>Bound from the "Database" section of database.json (see that file for examples).</summary>
public class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>Sqlite, SqlServer or Postgres.</summary>
    public string Provider { get; set; } = "Sqlite";

    public string ConnectionString { get; set; } = "Data Source=catalogo.db";

    /// <summary>
    /// Creates the database and the Items table when the database does not exist yet.
    /// For an existing database, run the matching script in /database instead.
    /// </summary>
    public bool CreateIfMissing { get; set; } = true;

    public SeedOptions Seed { get; set; } = new();

    public class SeedOptions
    {
        /// <summary>Loads the seed file only when the Items table is empty.</summary>
        public bool Enabled { get; set; } = true;
        public string File { get; set; } = "seed.json";
    }

    public static DatabaseOptions From(IConfiguration config) =>
        config.GetSection(Section).Get<DatabaseOptions>() ?? new DatabaseOptions();
}
