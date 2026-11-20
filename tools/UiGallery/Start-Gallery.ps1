param([switch]$CompactInfo, [switch]$ScheduleExport)

$ErrorActionPreference = 'Stop'
$galleryProject = Join-Path $PSScriptRoot 'UiGallery.csproj'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$localTools = Join-Path $repositoryRoot '.tools'
$dotnetExecutable = Join-Path $localTools 'dotnet\dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnetExecutable)) {
    $gitCommand = Get-Command git -ErrorAction SilentlyContinue
    if ($gitCommand) {
        $commonGitDirectory = & $gitCommand.Source -C $repositoryRoot rev-parse --path-format=absolute --git-common-dir
        if ($LASTEXITCODE -eq 0) {
            $localTools = Join-Path (Split-Path $commonGitDirectory -Parent) '.tools'
            $dotnetExecutable = Join-Path $localTools 'dotnet\dotnet.exe'
        }
    }
}
if (Test-Path -LiteralPath $dotnetExecutable) {
    $env:DOTNET_ROOT = Split-Path $dotnetExecutable -Parent
    $env:DOTNET_CLI_HOME = Join-Path $localTools 'cli'
    $env:NUGET_PACKAGES = Join-Path $localTools 'packages'
} else {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnetCommand) {
        throw 'Do uruchomienia galerii wymagany jest .NET SDK 10. Zainstaluj SDK lub umieść je w .tools/dotnet.'
    }
    $dotnetExecutable = $dotnetCommand.Source
}
$supportedSdk = & $dotnetExecutable --list-sdks | Where-Object { $_ -match '^(\d+)\.' -and [int]$Matches[1] -ge 10 }
if (-not $supportedSdk) {
    throw 'Brak .NET SDK 10 lub nowszego. Samo środowisko uruchomieniowe .NET nie wystarcza do zbudowania galerii.'
}
if ($ScheduleExport) {
    & $dotnetExecutable run --project $galleryProject --configuration Release -- --schedule-export
} elseif ($CompactInfo) {
    & $dotnetExecutable run --project $galleryProject --configuration Release -- --compact-info
} else {
    & $dotnetExecutable run --project $galleryProject --configuration Release
}
if ($LASTEXITCODE -ne 0) {
    throw "Nie udało się uruchomić galerii (kod $LASTEXITCODE)."
}
