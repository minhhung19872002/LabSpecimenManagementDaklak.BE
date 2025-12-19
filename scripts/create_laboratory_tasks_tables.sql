-- Create LaboratoryTasks table
CREATE TABLE IF NOT EXISTS "AppLaboratoryTasks" (
    "Id" uuid NOT NULL,
    "TestRequestId" uuid NOT NULL,
    "LaboratoryId" uuid NOT NULL,
    "ContractNumber" character varying(100),
    "TotalSampleCount" integer NOT NULL DEFAULT 0,
    "TechnicalManagerId" uuid,
    "TransferDate" timestamp without time zone,
    "RequestCompletionDate" timestamp without time zone,
    "TestDate" timestamp without time zone,
    "Status" integer NOT NULL DEFAULT 0,
    "Notes" character varying(2000),
    "ExtraProperties" text,
    "ConcurrencyStamp" character varying(40),
    "CreationTime" timestamp without time zone NOT NULL,
    "CreatorId" uuid,
    "LastModificationTime" timestamp without time zone,
    "LastModifierId" uuid,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeleterId" uuid,
    "DeletionTime" timestamp without time zone,
    CONSTRAINT "PK_AppLaboratoryTasks" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AppLaboratoryTasks_AppTestRequests_TestRequestId" FOREIGN KEY ("TestRequestId") REFERENCES "AppTestRequests" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AppLaboratoryTasks_AppLaboratories_LaboratoryId" FOREIGN KEY ("LaboratoryId") REFERENCES "AppLaboratories" ("Id") ON DELETE CASCADE
);

-- Create indexes for LaboratoryTasks
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTasks_TestRequestId" ON "AppLaboratoryTasks" ("TestRequestId");
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTasks_LaboratoryId" ON "AppLaboratoryTasks" ("LaboratoryId");
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTasks_TechnicalManagerId" ON "AppLaboratoryTasks" ("TechnicalManagerId");
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTasks_Status" ON "AppLaboratoryTasks" ("Status");
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTasks_TransferDate" ON "AppLaboratoryTasks" ("TransferDate");
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTasks_RequestCompletionDate" ON "AppLaboratoryTasks" ("RequestCompletionDate");

-- Create LaboratoryTaskIndicators table (Junction table)
CREATE TABLE IF NOT EXISTS "AppLaboratoryTaskIndicators" (
    "Id" uuid NOT NULL,
    "LaboratoryTaskId" uuid NOT NULL,
    "TestRequestSampleIndicatorId" uuid NOT NULL,
    CONSTRAINT "PK_AppLaboratoryTaskIndicators" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AppLaboratoryTaskIndicators_AppLaboratoryTasks_LaboratoryTaskId" FOREIGN KEY ("LaboratoryTaskId") REFERENCES "AppLaboratoryTasks" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AppLaboratoryTaskIndicators_AppTestRequestSampleIndicators_TestRequestSampleIndicatorId" FOREIGN KEY ("TestRequestSampleIndicatorId") REFERENCES "AppTestRequestSampleIndicators" ("Id") ON DELETE CASCADE
);

-- Create indexes for LaboratoryTaskIndicators
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTaskIndicators_LaboratoryTaskId" ON "AppLaboratoryTaskIndicators" ("LaboratoryTaskId");
CREATE INDEX IF NOT EXISTS "IX_AppLaboratoryTaskIndicators_TestRequestSampleIndicatorId" ON "AppLaboratoryTaskIndicators" ("TestRequestSampleIndicatorId");

-- Insert into EF Migrations History (optional - to mark as migrated)
-- INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20241219_AddLaboratoryTasks', '9.0.0');
