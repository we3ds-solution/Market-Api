# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Cache invalidation: Force fresh NuGet restore on package changes
# Copy solution and source project files (tests not needed for Docker build)
COPY Market.sln .
COPY src/Market.Domain/Market.Domain.csproj src/Market.Domain/
COPY src/Market.Application/Market.Application.csproj src/Market.Application/
COPY src/Market.Infrastructure/Market.Infrastructure.csproj src/Market.Infrastructure/
COPY src/Market.API/Market.API.csproj src/Market.API/

# Restore dependencies for source projects only
RUN dotnet restore "src/Market.API/Market.API.csproj"

# Copy source code
COPY src/ src/

# Build only API project for production
RUN dotnet build "src/Market.API/Market.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/Market.API/Market.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Market.API.dll"]
