$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$packageDir = Join-Path $root "packages"

# 清理旧包
Write-Host "Cleaning packages..."

if (Test-Path $packageDir)
{
    Remove-Item $packageDir -Recurse -Force
}

New-Item $packageDir -ItemType Directory | Out-Null

Write-Host "Cleaning solution..."
dotnet clean "$root\PbLite.slnx" -c Release

Write-Host "Building..."
dotnet build "$root\PbLite.slnx" -c Release --no-restore

Write-Host "Packing..."
dotnet pack "$root\src\PbLite.Core\PbLite.Core.csproj"  -c Release --no-build -o $packageDir
dotnet pack "$root\src\PbLite.Generator\PbLite.Generator.csproj" -c Release --no-build -o $packageDir
dotnet pack "$root\src\PbLite.ProtoGen\PbLite.ProtoGen.csproj" -c Release --no-build -o $packageDir
dotnet pack "$root\src\PbLite\PbLite.csproj"  -c Release --no-build -o $packageDir

Write-Host ""
Write-Host "Packages:"
Get-ChildItem "$packageDir\*.nupkg"
Write-Host ""

$confirm = Read-Host "Type YES to publish to NuGet"
if ($confirm -ne "YES")
{
    Write-Host "Publish cancelled."
    exit 0
}

Write-Host "Pushing..."

$apiKey = $env:NUGET_API_KEY

if ([string]::IsNullOrEmpty($apiKey))
{
    throw "NUGET_API_KEY is missing"
}

Get-ChildItem "$packageDir\*.nupkg" |
ForEach-Object {
    Write-Host "Pushing $($_.Name)..."
    dotnet nuget push $_.FullName --api-key $apiKey --source https://api.nuget.org/v3/index.json --skip-duplicate
}

Write-Host ""
Write-Host "Done."