-- Items table for SQLite. Only needed if you create the .db file yourself;
-- by default the API creates it (CreateIfMissing=true).
CREATE TABLE IF NOT EXISTS "Items" (
    "Id"          INTEGER NOT NULL CONSTRAINT "PK_Items" PRIMARY KEY AUTOINCREMENT,
    "Name"        TEXT NOT NULL,
    "Type"        TEXT NOT NULL,  -- Feature | Service | Tool
    "Status"      TEXT NOT NULL,  -- Active | InDevelopment | Deprecated
    "Description" TEXT NOT NULL,
    "OwnerTeam"   TEXT NOT NULL,
    "Contact"     TEXT NOT NULL,
    "RepoUrl"     TEXT NULL,
    "DocsUrl"     TEXT NULL,
    "Tags"        TEXT NOT NULL DEFAULT '[]',  -- JSON array
    "CreatedAt"   TEXT NOT NULL,
    "UpdatedAt"   TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Items_Name" ON "Items" ("Name");
CREATE INDEX IF NOT EXISTS "IX_Items_OwnerTeam" ON "Items" ("OwnerTeam");
