$ErrorActionPreference = "Stop"

dotnet tool install --global Cake.Tool --version 6.3.0
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# update installs it too, and moves an older global vpk forward
dotnet tool update --global vpk --version 1.2.158
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Pass all script arguments to dotnet-cake
dotnet-cake --configuration=Release @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }