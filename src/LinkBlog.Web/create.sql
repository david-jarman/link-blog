CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Posts" (
    "Id" text NOT NULL,
    "Title" text NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "Link" text,
    "LinkTitle" text,
    "Contents" text,
    CONSTRAINT "PK_Posts" PRIMARY KEY ("Id")
);

CREATE TABLE "Tags" (
    "Id" text NOT NULL,
    "Name" text NOT NULL,
    CONSTRAINT "PK_Tags" PRIMARY KEY ("Id")
);

CREATE TABLE "PostTag" (
    "PostsId" text NOT NULL,
    "TagsId" text NOT NULL,
    CONSTRAINT "PK_PostTag" PRIMARY KEY ("PostsId", "TagsId"),
    CONSTRAINT "FK_PostTag_Posts_PostsId" FOREIGN KEY ("PostsId") REFERENCES "Posts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PostTag_Tags_TagsId" FOREIGN KEY ("TagsId") REFERENCES "Tags" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_PostTag_TagsId" ON "PostTag" ("TagsId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250215004115_InitialCreate', '9.0.2');

COMMIT;


