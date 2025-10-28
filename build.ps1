$ErrorActionPreference = "Stop"

dotnet tool install --global Cake.Tool --version 5.1.0
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet tool install --global vpk --version 0.0.1298
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Pass all script arguments to dotnet-cake
dotnet-cake --configuration=Release @args
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }