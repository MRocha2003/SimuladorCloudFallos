# Ejecuta las pruebas unitarias y de integración con medición de cobertura.
# Uso (PowerShell, desde la raíz del repositorio):  ./run-tests.ps1
$ErrorActionPreference = "Stop"
Remove-Item -Recurse -Force TestResults -ErrorAction SilentlyContinue

dotnet test tests/SimuladorCloud.Tests/SimuladorCloud.Tests.csproj `
    --logger "console;verbosity=normal" `
    --logger "trx;LogFileName=resultados.trx" `
    --collect:"XPlat Code Coverage" `
    --results-directory TestResults

# Reporte HTML de cobertura (instala la herramienta la primera vez)
if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    dotnet tool install -g dotnet-reportgenerator-globaltool
}
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" `
    -targetdir:"TestResults/CoverageReport" `
    -reporttypes:"Html;TextSummary" `
    -classfilters:"-*.Shared.*;-SimuladorCloud.Client.Program*;-SimuladorCloud.Client.App*"

Get-Content TestResults/CoverageReport/Summary.txt
Write-Host "`nAbra TestResults/CoverageReport/index.html para ver el reporte completo."
