# Docker Setup for Market API

This guide explains how to run the Market API application using Docker and Docker Compose with SQL Server.

## Prerequisites

- Docker Desktop installed on your system
- Docker Compose (included with Docker Desktop)

## Quick Start

### 1. Build and Run with Docker Compose

From the project root directory, run:

```bash
docker-compose up --build
```

This command will:
- Build the Market API Docker image
- Pull the SQL Server image
- Start both containers
- Create a network for communication between containers
- Set up a persistent volume for SQL Server data

### 2. Access the Application

- **API**: http://localhost:5000
- **API (HTTPS)**: https://localhost:5001 (if configured)
- **SQL Server**: localhost:1433
- **Swagger UI**: http://localhost:5000/swagger

### 3. Test the API

You can test the API endpoints using curl, Postman, or the provided `.http` file:

```bash
# Get all products
curl http://localhost:5000/api/products

# Get product by ID
curl http://localhost:5000/api/products/1

# Create a new product
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Sample Product","price":29.99,"categoryId":1,"vendorId":1,"quantity":10}'
```

## Docker Configuration Details

### Services

1. **sqlserver**
   - Image: mcr.microsoft.com/mssql/server:2022-latest
   - Port: 1433
   - Credentials: SA user with password from `.env` file
   - Volume: Persistent SQL Server data (`sqlserver_data`)

2. **market-api**
   - Built from Dockerfile in project root
   - Ports: 5000 (HTTP), 5001 (HTTPS)
   - Environment: Development (configurable via `.env`)
   - Auto-connects to SQL Server container via connection string

### Environment Variables

Configure these in `docker-compose.yml` or `.env` file:

- `ASPNETCORE_ENVIRONMENT`: Set to Development
- `ASPNETCORE_URLS`: HTTP endpoint configuration
- `ConnectionStrings__DefaultConnection`: SQL Server connection string
- `SA_PASSWORD`: SQL Server SA user password (must be >8 chars, mixed case, numbers, symbols)

## Setup Instructions

### 1. Create `.env` File

Copy `.env.example` to `.env` and update the SQL Server password:

```bash
cp .env.example .env
```

Edit `.env` and set a strong password:
```
SA_PASSWORD=YourStrongP@ssw0rd123!
```

### 2. Run Docker Compose

```bash
docker-compose up --build
```

The first run will:
- Pull SQL Server image (~2-3 GB)
- Build the Market API image
- Initialize the SQL Server database
- Apply EF Core migrations automatically (if configured)

### 3. Verify Services

```bash
# Check running containers
docker ps

# Expected output should show:
# - sqlserver container running on port 1433
# - market-api container running on port 5000
```

## Useful Commands

### Start containers in detached mode
```bash
docker-compose up -d
```

### Stop containers
```bash
docker-compose down
```

### Stop containers and remove volumes (deletes data)
```bash
docker-compose down -v
```

### View logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f market-api
docker-compose logs -f sqlserver
```

### Rebuild containers
```bash
docker-compose up --build
```

### Connect to SQL Server container
```bash
# Using sqlcmd
docker exec -it market-api-sqlserver-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "YourStrongP@ssw0rd123!"
```

## Troubleshooting

### Port Already in Use
If ports 5000, 5001, or 1433 are already in use, modify the port mappings in `docker-compose.yml`:

```yaml
services:
  market-api:
    ports:
      - "5002:8080"  # Change 5000 to 5002
      - "5003:8081"  # Change 5001 to 5003
  
  sqlserver:
    ports:
      - "1434:1433"  # Change 1433 to 1434
```

### Container Won't Start
Check logs for errors:
```bash
docker-compose logs market-api
docker-compose logs sqlserver
```

### SQL Server Connection Issues
Ensure the SQL Server container is running and accepting connections:
```bash
docker exec -it market-api-sqlserver-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "YourStrongP@ssw0rd123!" -Q "SELECT 1"
```

### Reset Everything
To start fresh (removes volumes and all data):
```bash
docker-compose down -v
docker-compose up --build
```

## Production Considerations

For production deployment, consider:

1. **Security**: 
   - Use strong SA password (>12 chars, mixed case, symbols, numbers)
   - Use Docker secrets instead of environment variables
   - Enable SQL Server SSL/TLS

2. **HTTPS**: 
   - Configure proper SSL certificates
   - Use reverse proxy (nginx, Traefik)

3. **Environment**: 
   - Set `ASPNETCORE_ENVIRONMENT=Production`
   - Enable Kestrel HTTPS

4. **Secrets**: 
   - Use Docker secrets or Azure Key Vault
   - Never commit `.env` file with real credentials

5. **Monitoring**: 
   - Add health checks
   - Configure logging to centralized service (ELK, Splunk)
   - Monitor container resource usage

6. **Backup**: 
   - Implement SQL Server backup strategy
   - Store backups outside container volume
   - Test recovery procedures

7. **Scaling**: 
   - Use Kubernetes (not Docker Compose)
   - Implement database connection pooling
   - Use load balancer for API

## Network Architecture

```
┌─────────────────────────────────────┐
│   Docker Network (bridge)           │
│                                     │
│  ┌──────────────┐  ┌─────────────┐ │
│  │  Market API  │  │  SQL Server │ │
│  │  Port: 5000  │──│  Port: 1433 │ │
│  └──────────────┘  └─────────────┘ │
│         │                           │
└─────────┼───────────────────────────┘
          │
    Host Ports
    5000 (HTTP)
    5001 (HTTPS)
    1433 (SQL Server)
```

## References

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Reference](https://docs.docker.com/compose/compose-file/)
- [SQL Server in Docker](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
- [ASP.NET Core Docker](https://docs.microsoft.com/en-us/dotnet/core/docker/build-container)
