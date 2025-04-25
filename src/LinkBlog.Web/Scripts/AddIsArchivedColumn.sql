START TRANSACTION;

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
    VALUES ('20250425033914_AddIsArchivedColumn', '9.0.4');
    END IF;
END $EF$;
COMMIT;

