# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files for dependency caching
COPY *.sln ./
COPY src/LabSpecimenManagement.Domain/LabSpecimenManagement.Domain.csproj ./src/LabSpecimenManagement.Domain/
COPY src/LabSpecimenManagement.Application.Contracts/LabSpecimenManagement.Application.Contracts.csproj ./src/LabSpecimenManagement.Application.Contracts/
COPY src/LabSpecimenManagement.Application/LabSpecimenManagement.Application.csproj ./src/LabSpecimenManagement.Application/
COPY src/LabSpecimenManagement.EntityFrameworkCore/LabSpecimenManagement.EntityFrameworkCore.csproj ./src/LabSpecimenManagement.EntityFrameworkCore/
COPY src/LabSpecimenManagement.HttpApi/LabSpecimenManagement.HttpApi.csproj ./src/LabSpecimenManagement.HttpApi/
COPY src/LabSpecimenManagement.HttpApi.Host/LabSpecimenManagement.HttpApi.Host.csproj ./src/LabSpecimenManagement.HttpApi.Host/
COPY src/LabSpecimenManagement.DbMigrator/LabSpecimenManagement.DbMigrator.csproj ./src/LabSpecimenManagement.DbMigrator/

# Restore dependencies
RUN dotnet restore

# Copy the entire source code
COPY . .

# Build and publish the HttpApi.Host project
WORKDIR /src/src/LabSpecimenManagement.HttpApi.Host
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published application from build stage
COPY --from=build /app/publish .

# Create directories for logs and uploaded images
RUN mkdir -p /app/Logs /app/wwwroot/images

# Set environment variables (will be overridden by docker-compose/deployment)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5002

# Expose port
EXPOSE 5002

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:5002/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "LabSpecimenManagement.HttpApi.Host.dll"]
