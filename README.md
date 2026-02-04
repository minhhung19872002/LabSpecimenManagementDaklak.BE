# Lab Specimen Management System - Backend API

A comprehensive laboratory specimen management system built with ABP Framework and .NET 9.

> **Note**: Database migrations run automatically on deployment via init container.

## Quick Start

### Option 1: Using Start Script (Recommended)

```bash
./start-dev.sh
```

This will automatically:
- Start Docker services
- Run migrations if needed
- Start the backend API

### Option 2: Manual Start

```bash
# 1. Start Docker services
docker compose up -d

# 2. Run migrations
cd src/LabSpecimenManagement.DbMigrator
NUGET_PACKAGES=../../.nuget-packages dotnet run

# 3. Start API
cd ../LabSpecimenManagement.HttpApi.Host
NUGET_PACKAGES=../../.nuget-packages dotnet run
```

**Backend API**: http://localhost:5002
**Swagger UI**: http://localhost:5002/swagger

### Stop Services

```bash
./stop-dev.sh
```

## Pre-requirements

* [.NET 9.0+ SDK](https://dotnet.microsoft.com/download/dotnet)
* [Docker & Docker Compose](https://docs.docker.com/get-docker/)
* [Node v18 or 20](https://nodejs.org/en) (for frontend)

## Default Credentials

- **Username**: `admin`
- **Password**: `1q2w3E*`

See [ACCOUNTS.md](./ACCOUNTS.md) for detailed login information and testing instructions.

## Tech Stack

- **Framework**: ABP Framework 9.0.0
- **.NET**: 9.0
- **Database**: PostgreSQL 15
- **Cache**: Redis 7
- **Document Store**: MongoDB 7
- **ORM**: Entity Framework Core
- **Authentication**: OpenIddict

## Ports

- Backend API: **5002**
- PostgreSQL: **5432**
- Redis: **6379**
- MongoDB: **27017**

## Development

See [DEVELOPMENT.md](./DEVELOPMENT.md) for detailed development guide including:
- Project structure
- Development workflow
- Adding new entities & API endpoints
- Database management
- Troubleshooting
- Testing

## Docker Services

The project includes Docker Compose configuration for required services:

```yaml
services:
  - PostgreSQL 15 (Database)
  - Redis 7 (Cache)
  - MongoDB 7 (Document Store)
```

Start all services:
```bash
docker compose up -d
```

Stop all services:
```bash
docker compose down
```

## Resources

- [DEVELOPMENT.md](./DEVELOPMENT.md) - Detailed development guide
- [ABP Framework Documentation](https://abp.io/docs/latest)
- [Frontend Repository](../lab-specimen-management-fe/)

## License

[Your License Here]
