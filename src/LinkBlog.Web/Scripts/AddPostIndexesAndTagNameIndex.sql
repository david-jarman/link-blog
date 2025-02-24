START TRANSACTION;
CREATE UNIQUE INDEX "IX_Tags_Name" ON "Tags" ("Name");

CREATE INDEX "IX_Posts_Date" ON "Posts" ("Date");

CREATE UNIQUE INDEX "IX_Posts_ShortTitle" ON "Posts" ("ShortTitle");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250224000450_AddPostIndexesAndTagNameIndex', '9.0.2');

COMMIT;


