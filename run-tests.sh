#!/usr/bin/env bash
# Ejecuta las pruebas unitarias y de integración con medición de cobertura.
# Uso (desde la raíz del repositorio):  ./run-tests.sh
set -euo pipefail
rm -rf TestResults

dotnet test tests/SimuladorCloud.Tests/SimuladorCloud.Tests.csproj \
    --logger "console;verbosity=normal" \
    --logger "trx;LogFileName=resultados.trx" \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults

command -v reportgenerator >/dev/null || dotnet tool install -g dotnet-reportgenerator-globaltool
export PATH="$PATH:$HOME/.dotnet/tools"
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" \
    -targetdir:"TestResults/CoverageReport" \
    -reporttypes:"Html;TextSummary" \
    -classfilters:"-*.Shared.*;-SimuladorCloud.Client.Program*;-SimuladorCloud.Client.App*"

cat TestResults/CoverageReport/Summary.txt
echo "Abra TestResults/CoverageReport/index.html para ver el reporte completo."
