[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $AndroidSdkDirectory,
    [Parameter(Mandatory)]
    [string] $JavaSdkDirectory,
    [string] $Dotnet = 'dotnet'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$templateDirectory = Join-Path $PSScriptRoot 'workmanager'
$manifest = Get-Content (Join-Path $templateDirectory 'inputs.json') -Raw | ConvertFrom-Json
$buildRoot = Join-Path $repositoryRoot 'artifacts/workmanager'
$workspace = Join-Path $buildRoot ([Guid]::NewGuid().ToString('N'))
$feed = Join-Path $buildRoot 'feed'
New-Item -ItemType Directory -Path $workspace, $feed -Force | Out-Null

foreach ($fileName in @('Work.Runtime.csproj', 'packages.lock.json', 'global.json', 'NuGet.Config')) {
    Copy-Item -LiteralPath (Join-Path $templateDirectory $fileName) -Destination $workspace
}
foreach ($inputFile in $manifest.Files) {
    $destination = Join-Path $workspace $inputFile.Path
    Invoke-WebRequest -Uri $inputFile.Uri -OutFile $destination
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $inputFile.SHA256) {
        throw "Upstream input hash mismatch: $($inputFile.Path)"
    }
}

Push-Location $workspace
try {
    $sdk = Get-Content './global.json' -Raw | ConvertFrom-Json
    $sdkVersion = (& $Dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdkVersion -ne $sdk.sdk.version) {
        throw "Expected .NET SDK $($sdk.sdk.version); found $sdkVersion."
    }
    $project = Join-Path $workspace 'Work.Runtime.csproj'
    $workloadVersion = (& $Dotnet msbuild $project -getProperty:AndroidNETSdkVersion).Trim()
    if ($LASTEXITCODE -ne 0 -or $workloadVersion -ne $manifest.AndroidWorkload) {
        throw "Expected Android workload $($manifest.AndroidWorkload); found $workloadVersion."
    }
    $properties = @("-p:AndroidSdkDirectory=$AndroidSdkDirectory", "-p:JavaSdkDirectory=$JavaSdkDirectory")
    & $Dotnet restore $project --locked-mode @properties *> './restore.log'
    if ($LASTEXITCODE -ne 0) {
        throw "Binding restore failed; see $workspace/restore.log."
    }
    & $Dotnet pack $project --no-restore --configuration Release --output $feed @properties *> './pack.log'
    if ($LASTEXITCODE -ne 0) {
        throw "Binding pack failed; see $workspace/pack.log."
    }
    Write-Output "WorkManager binding built into $feed."
}
finally {
    Pop-Location
}
