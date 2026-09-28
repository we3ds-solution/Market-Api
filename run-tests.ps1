$ErrorActionPreference = 'Stop'
Set-Location "c:\Users\mosta\Downloads\projects\Market-Api"

Write-Output "=== BUILD ==="
& dotnet build Market.sln -v quiet 2>&1 | ForEach-Object { Write-Output $_ }
if ($LASTEXITCODE -ne 0) { Write-Output "BUILD FAILED"; exit 1 }

Write-Output "=== TESTS: CreateOrderHandlerTests ==="
& dotnet test tests/Market.Application.Tests/Market.Application.Tests.csproj --no-build -v normal --filter "FullyQualifiedName~CreateOrderHandlerTests" 2>&1 | ForEach-Object { Write-Output $_ }
exit $LASTEXITCODE
