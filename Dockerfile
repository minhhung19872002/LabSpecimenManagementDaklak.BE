# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy ALL project files first (this allows Docker to cache dependencies)
COPY src/ ./src/
COPY test/ ./test/
COPY *.sln ./
COPY *.props ./

# Restore dependencies for the main API project
RUN dotnet restore src/LabSpecimenManagement.HttpApi.Host/LabSpecimenManagement.HttpApi.Host.csproj

# Build and publish the HttpApi.Host project
WORKDIR /src/src/LabSpecimenManagement.HttpApi.Host
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

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
