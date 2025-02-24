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
    VALUES ('20250215004115_InitialCreate', '9.0.2');
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
    VALUES ('20250220042255_AddShortTitle', '9.0.2');
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
    VALUES ('20250224000450_AddPostIndexesAndTagNameIndex', '9.0.2');
    END IF;
END $EF$;
COMMIT;


