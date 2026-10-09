-- Items table for SQL Server. Run once on an EXISTING database
-- (with CreateIfMissing=true the API creates a brand-new database by itself).
IF OBJECT_ID(N'dbo.Items', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Items (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Items PRIMARY KEY,
        Name        NVARCHAR(120)  NOT NULL,
        Type        NVARCHAR(20)   NOT NULL,  -- Feature | Service | Tool
        Status      NVARCHAR(20)   NOT NULL,  -- Active | InDevelopment | Deprecated
        Description NVARCHAR(1000) NOT NULL,
        OwnerTeam   NVARCHAR(120)  NOT NULL,
        Contact     NVARCHAR(120)  NOT NULL,
        RepoUrl     NVARCHAR(500)  NULL,
        DocsUrl     NVARCHAR(500)  NULL,
        Tags        NVARCHAR(MAX)  NOT NULL CONSTRAINT DF_Items_Tags DEFAULT N'[]', -- JSON array, e.g. ["python","auth"]
        CreatedAt   DATETIME2      NOT NULL,
        UpdatedAt   DATETIME2      NOT NULL
    );
    CREATE INDEX IX_Items_Name ON dbo.Items (Name);
    CREATE INDEX IX_Items_OwnerTeam ON dbo.Items (OwnerTeam);
END;

-- Example row (dates in UTC):
-- INSERT INTO dbo.Items (Name, Type, Status, Description, OwnerTeam, Contact, Tags, CreatedAt, UpdatedAt)
-- VALUES (N'Exportador a Excel', N'Tool', N'Active', N'Exporta reportes a xlsx', N'Data', N'Carlos Quiroz', N'["excel","python"]', SYSUTCDATETIME(), SYSUTCDATETIME());
