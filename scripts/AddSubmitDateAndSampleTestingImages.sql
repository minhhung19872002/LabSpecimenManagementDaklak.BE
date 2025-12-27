-- Migration: AddSubmitDateAndSampleTestingImages
-- Run this SQL script manually on the PostgreSQL database

-- 1. Thêm cột SubmitDate vào bảng AppLaboratoryTaskIndicators
ALTER TABLE "AppLaboratoryTaskIndicators"
ADD COLUMN IF NOT EXISTS "SubmitDate" timestamp without time zone NULL;

-- 2. Tạo bảng AppSampleTestingImages (nếu chưa tồn tại)
CREATE TABLE IF NOT EXISTS "AppSampleTestingImages" (
    "Id" uuid NOT NULL,
    "LaboratoryTaskId" uuid NOT NULL,
    "SampleId" uuid NOT NULL,
    "FileId" character varying(100) NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "FilePath" character varying(500) NOT NULL,
    "FileSize" bigint NOT NULL,
    "ImageType" integer NOT NULL,
    "CreationTime" timestamp without time zone NOT NULL,
    "CreatorId" uuid NULL,
    CONSTRAINT "PK_AppSampleTestingImages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AppSampleTestingImages_AppLaboratoryTasks_LaboratoryTaskId"
        FOREIGN KEY ("LaboratoryTaskId")
        REFERENCES "AppLaboratoryTasks" ("Id")
        ON DELETE CASCADE
);

-- 3. Tạo indexes
CREATE INDEX IF NOT EXISTS "IX_AppSampleTestingImages_LaboratoryTaskId"
ON "AppSampleTestingImages" ("LaboratoryTaskId");

CREATE INDEX IF NOT EXISTS "IX_AppSampleTestingImages_SampleId"
ON "AppSampleTestingImages" ("SampleId");

CREATE INDEX IF NOT EXISTS "IX_AppSampleTestingImages_LaboratoryTaskId_SampleId_ImageType"
ON "AppSampleTestingImages" ("LaboratoryTaskId", "SampleId", "ImageType");

-- 4. Insert record vào __EFMigrationsHistory (nếu cần track)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251227140000_AddSubmitDateAndSampleTestingImages', '9.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20251227140000_AddSubmitDateAndSampleTestingImages'
);

-- Done!
