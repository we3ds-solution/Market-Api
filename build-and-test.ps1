Set-Location "c:\Users\mosta\Downloads\projects\Market-Api"
Write-Host "=== BUILD ===" -ForegroundColor Cyan
dotnet build Market.sln -v quiet
if ($LASTEXITCODE -ne 0) { Write-Host "BUILD FAILED" -ForegroundColor Red; exit 1 }

Write-Host "=== TESTS ===" -ForegroundColor Cyan
dotnet test tests/Market.Application.Tests/Market.Application.Tests.csproj --no-build -v normal --filter "FullyQualifiedName~CreateOrderHandlerTests"
exit $LASTEXITCODE
