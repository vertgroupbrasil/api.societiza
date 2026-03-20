# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY Societiza.sln ./
COPY src/Core/Core.Application/Core.Application.csproj src/Core/Core.Application/
COPY src/Core/Core.Domain/Core.Domain.csproj src/Core/Core.Domain/
COPY src/Core/Core.Infrastructure/Core.Infrastructure.csproj src/Core/Core.Infrastructure/
COPY src/Core/Core.Persistence/Core.Persistence.csproj src/Core/Core.Persistence/
COPY src/Core/Core.Shared/Core.Shared.csproj src/Core/Core.Shared/
COPY src/Core/Core.Endpoints/Core.Endpoints.csproj src/Core/Core.Endpoints/

COPY src/Modules/Accountancy/Accountancy.Application/Accountancy.Application.csproj src/Modules/Accountancy/Accountancy.Application/
COPY src/Modules/Accountancy/Accountancy.Domain/Accountancy.Domain.csproj src/Modules/Accountancy/Accountancy.Domain/
COPY src/Modules/Accountancy/Accountancy.Infrastructure/Accountancy.Infrastructure.csproj src/Modules/Accountancy/Accountancy.Infrastructure/
COPY src/Modules/Accountancy/Accountancy.Persistence/Accountancy.Persistence.csproj src/Modules/Accountancy/Accountancy.Persistence/
COPY src/Modules/Accountancy/Accountancy.Shared/Accountancy.Shared.csproj src/Modules/Accountancy/Accountancy.Shared/

COPY src/Modules/WorkflowTemplate/WorkflowTemplate.Application/WorkflowTemplate.Application.csproj src/Modules/WorkflowTemplate/WorkflowTemplate.Application/
COPY src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/WorkflowTemplate.Domain.csproj src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/
COPY src/Modules/WorkflowTemplate/WorkflowTemplate.Infrastructure/WorkflowTemplate.Infrastructure.csproj src/Modules/WorkflowTemplate/WorkflowTemplate.Infrastructure/
COPY src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence/WorkflowTemplate.Persistence.csproj src/Modules/WorkflowTemplate/WorkflowTemplate.Persistence/
COPY src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/WorkflowTemplate.Shared.csproj src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/

COPY src/Web/Web.csproj src/Web/

# Restore dependencies (cached unless .csproj files change)
RUN dotnet restore src/Web/Web.csproj

# Copy source code
COPY src/ src/

# Publish
RUN dotnet publish src/Web/Web.csproj -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Configure environment
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app .

USER appuser
ENTRYPOINT ["dotnet", "Web.dll"]