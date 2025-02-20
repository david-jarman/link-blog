START TRANSACTION;

UPDATE "Posts" SET "Contents" = '' WHERE "Contents" IS NULL;
ALTER TABLE "Posts" ALTER COLUMN "Contents" SET NOT NULL;
ALTER TABLE "Posts" ALTER COLUMN "Contents" SET DEFAULT '';

ALTER TABLE "Posts" ADD "ShortTitle" text NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250220042255_AddShortTitle', '9.0.2');

COMMIT;
