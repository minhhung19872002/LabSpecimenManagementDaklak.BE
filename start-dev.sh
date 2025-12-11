#!/bin/bash

echo "🚀 Starting Lab Specimen Management Backend..."
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Start Docker services
echo "📦 Starting Docker services (PostgreSQL, Redis, MongoDB)..."
docker compose up -d

# Wait for PostgreSQL to be ready
echo "⏳ Waiting for PostgreSQL to be ready..."
sleep 5

# Check if migrations need to run
echo "🔍 Checking database..."
if ! docker exec lab_postgres psql -U postgres -d LabSpecimenManagement -c "SELECT 1 FROM pg_tables WHERE tablename='__EFMigrationsHistory';" > /dev/null 2>&1; then
    echo "📝 Running database migrations..."
    cd src/LabSpecimenManagement.DbMigrator
    NUGET_PACKAGES=../../.nuget-packages dotnet run
    cd ../..
    echo "✅ Migrations completed"
else
    echo "✅ Database already migrated"
fi

# Start backend API
echo ""
echo "🌐 Starting Backend API on http://localhost:5002..."
echo "📚 Swagger UI will be available at http://localhost:5002/swagger"
echo ""
echo "Press Ctrl+C to stop"
echo ""

cd src/LabSpecimenManagement.HttpApi.Host
NUGET_PACKAGES=../../.nuget-packages dotnet run
