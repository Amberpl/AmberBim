param([string]$DotNetPath = 'dotnet')

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
& $DotNetPath run --project (Join-Path $PSScriptRoot 'IconGenerator/IconGenerator.csproj') --configuration Release -- $repositoryRoot
if ($LASTEXITCODE -ne 0) { throw 'Ribbon icon generation failed.' }
