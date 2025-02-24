START TRANSACTION;
ALTER TABLE "Posts" ADD "UpdatedDate" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';


UPDATE "Posts"
SET "UpdatedDate" = "Date";


INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250224051317_AddUpdatedDate', '9.0.2');

COMMIT;


