#Requires -Version 7.0
<#
.SYNOPSIS
    Pre-push validation script matching CI parity.
.DESCRIPTION
    Runs the same restore/build/test/pack sequence as CI.
    Exit code 0 = all gates pass; non-zero = at least one gate failed.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'src/ManicTimeMcp.slnx'

function Invoke-Step {
    param([string]$Name, [scriptblock]$Command)
    Write-Host "`n=== $Name ===" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $Name (exit code $LASTEXITCODE)" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Invoke-Step 'Restore' { dotnet restore $solution }
Invoke-Step 'Build' { dotnet build $solution -c Release -warnaserror --no-restore }
Invoke-Step 'Test' { dotnet test --solution $solution -c Release --no-build }
Invoke-Step 'Pack' { dotnet pack $solution -c Release --no-build }

Write-Host "`nAll pre-push gates passed." -ForegroundColor Green
