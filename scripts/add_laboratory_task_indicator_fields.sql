-- SQL Script to add new fields to AppLaboratoryTaskIndicators table
-- Run this script to apply changes for Phase 6: Task Detail Page

-- Add new columns
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "MethodId" uuid NULL;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "TechnicianId" uuid NULL;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "Result" character varying(500) NULL;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "Unit" character varying(50) NULL;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "EntryDate" timestamp without time zone NULL;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "EnteredById" uuid NULL;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "ApprovalStatus" integer NOT NULL DEFAULT 0;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "ApprovalDate" timestamp without time zone NULL;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "ApprovedById" uuid NULL;
ALTER TABLE "AppLaboratoryTaskIndicators" ADD COLUMN IF NOT EXISTS "RejectionReason" character varying(1000) NULL;

-- Create indexes
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTaskIndicators_MethodId" ON "AppLaboratoryTaskIndicators" ("MethodId");
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTaskIndicators_TechnicianId" ON "AppLaboratoryTaskIndicators" ("TechnicianId");
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTaskIndicators_ApprovalStatus" ON "AppLaboratoryTaskIndicators" ("ApprovalStatus");

-- Add migration history record
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT '20251219120000_AddLaboratoryTaskIndicatorFields', '9.0.0'
WHERE NOT EXISTS (
    SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20251219120000_AddLaboratoryTaskIndicatorFields'
);
