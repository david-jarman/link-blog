CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250215004115_InitialCreate') THEN
    CREATE TABLE "Posts" (
        "Id" text NOT NULL,
        "Title" text NOT NULL,
        "Date" timestamp with time zone NOT NULL,
        "Link" text,
        "LinkTitle" text,
        "Contents" text,
        CONSTRAINT "PK_Posts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250215004115_InitialCreate') THEN
    CREATE TABLE "Tags" (
        "Id" text NOT NULL,
        "Name" text NOT NULL,
        CONSTRAINT "PK_Tags" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250215004115_InitialCreate') THEN
    CREATE TABLE "PostTag" (
        "PostsId" text NOT NULL,
        "TagsId" text NOT NULL,
        CONSTRAINT "PK_PostTag" PRIMARY KEY ("PostsId", "TagsId"),
        CONSTRAINT "FK_PostTag_Posts_PostsId" FOREIGN KEY ("PostsId") REFERENCES "Posts" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PostTag_Tags_TagsId" FOREIGN KEY ("TagsId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250215004115_InitialCreate') THEN
    CREATE INDEX "IX_PostTag_TagsId" ON "PostTag" ("TagsId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250215004115_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250215004115_InitialCreate', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250220042255_AddShortTitle') THEN
    UPDATE "Posts" SET "Contents" = '' WHERE "Contents" IS NULL;
    ALTER TABLE "Posts" ALTER COLUMN "Contents" SET NOT NULL;
    ALTER TABLE "Posts" ALTER COLUMN "Contents" SET DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250220042255_AddShortTitle') THEN
    ALTER TABLE "Posts" ADD "ShortTitle" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250220042255_AddShortTitle') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250220042255_AddShortTitle', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250224000450_AddPostIndexesAndTagNameIndex') THEN
    CREATE UNIQUE INDEX "IX_Tags_Name" ON "Tags" ("Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250224000450_AddPostIndexesAndTagNameIndex') THEN
    CREATE INDEX "IX_Posts_Date" ON "Posts" ("Date");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250224000450_AddPostIndexesAndTagNameIndex') THEN
    CREATE UNIQUE INDEX "IX_Posts_ShortTitle" ON "Posts" ("ShortTitle");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250224000450_AddPostIndexesAndTagNameIndex') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250224000450_AddPostIndexesAndTagNameIndex', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250224051317_AddUpdatedDate') THEN
    ALTER TABLE "Posts" ADD "UpdatedDate" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250224051317_AddUpdatedDate') THEN

        UPDATE "Posts"
        SET "UpdatedDate" = "Date";

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250224051317_AddUpdatedDate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250224051317_AddUpdatedDate', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250425033914_AddIsArchivedColumn') THEN
    ALTER TABLE "Posts" ADD "IsArchived" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250425033914_AddIsArchivedColumn') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250425033914_AddIsArchivedColumn', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251114050210_AddFullTextSearchVector') THEN
    ALTER TABLE "Posts" ADD "SearchVector" tsvector GENERATED ALWAYS AS (setweight(to_tsvector('english', COALESCE("Title", '')), 'A') ||
                      setweight(to_tsvector('english', COALESCE("LinkTitle", '')), 'B') ||
                      setweight(to_tsvector('english', COALESCE("Contents", '')), 'C')) STORED;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251114050210_AddFullTextSearchVector') THEN
    CREATE INDEX "IX_Posts_SearchVector" ON "Posts" USING GIN ("SearchVector");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251114050210_AddFullTextSearchVector') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20251114050210_AddFullTextSearchVector', '9.0.9');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260108000000_AddKarmaColumn') THEN
    ALTER TABLE "Posts" ADD "Karma" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260108000000_AddKarmaColumn') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260108000000_AddKarmaColumn', '9.0.9');
    END IF;
END $EF$;
COMMIT;

