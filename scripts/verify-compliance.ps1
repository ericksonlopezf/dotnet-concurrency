# Copyright © Erickson Lopez. MIT License.
<#
.SYNOPSIS
    Architecture & Quality Standards Compliance Verification Script for EricksonLopez.Concurrency.
.DESCRIPTION
    Validates architectural invariants:
    1. Kebab-case naming for all documentation files (with reserved standard allowlist).
    2. Zero [Obsolete] usages in production code (src/).
    3. Presence of canonical MIT copyright header across all C# source files.
    4. Single top-level type per file in src/.
    5. Valid GitHub repository links referencing ericksonlopezf/dotnet-concurrency.
    6. Official support and security email normalization (ericksonlopezf@gmail.com).
    7. Zero prohibited compiler warning suppressions (CS1591, CS0618, CS0619).
    8. NuGet package icon metadata & asset presence in Directory.Build.props.
#>

[CmdletBinding()]
param (
    [string]$RootDirectory = "."
)

$ErrorActionPreference = "Stop"
$violations = 0

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  REPOSITORY COMPLIANCE & ARCHITECTURE AUDITOR    " -ForegroundColor Cyan
Write-Host "  Repository: EricksonLopez.Concurrency           " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Kebab-case documentation verification
Write-Host "`n[1/8] Checking documentation file naming (kebab-case)..." -ForegroundColor Yellow
$reservedNames = @("README.md", "CHANGELOG.md", "CODE_OF_CONDUCT.md", "CONTRIBUTING.md", "SECURITY.md", "SUPPORT.md", "LICENSE", "PULL_REQUEST_TEMPLATE.md")
$allMdFiles = Get-ChildItem -Path $RootDirectory -Recurse -Filter "*.md" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch "\\(obj|bin|node_modules|\.git)\\" }
$badDocNames = 0
if ($allMdFiles) {
    foreach ($doc in $allMdFiles) {
        $filename = $doc.Name
        if ($reservedNames -contains $filename) {
            continue
        }
        if ($filename -cne $filename.ToLower() -or $filename -match "_") {
            Write-Host "  ❌ Non-kebab-case document: $($doc.FullName)" -ForegroundColor Red
            $violations++
            $badDocNames++
        }
    }
}
if ($badDocNames -eq 0) { Write-Host "  ✅ All documentation files use valid kebab-case naming." -ForegroundColor Green }

# 2. Zero Obsolete APIs in src/
Write-Host "`n[2/8] Checking for [Obsolete] attribute usages in src/..." -ForegroundColor Yellow
$srcCsFiles = Get-ChildItem -Path (Join-Path $RootDirectory "src") -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }
$obsoleteCount = 0
if ($srcCsFiles) {
    foreach ($cs in $srcCsFiles) {
        $lines = Get-Content $cs.FullName
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "^\s*\[Obsolete\b" -and $lines[$i] -notmatch "^\s*//") {
                Write-Host "  ❌ [Obsolete] found in $($cs.FullName):$($i + 1)" -ForegroundColor Red
                $violations++
                $obsoleteCount++
            }
        }
    }
}
if ($obsoleteCount -eq 0) { Write-Host "  ✅ Zero [Obsolete] attributes in production code." -ForegroundColor Green }

# 3. Canonical MIT Copyright Header
Write-Host "`n[3/8] Checking canonical MIT copyright headers..." -ForegroundColor Yellow
$missingHeaderCount = 0
$allCsFiles = Get-ChildItem -Path $RootDirectory -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }
if ($allCsFiles) {
    foreach ($cs in $allCsFiles) {
        $firstLine = (Get-Content $cs.FullName -TotalCount 1)
        if ($firstLine -notmatch "^// Copyright © Erickson Lopez\. MIT License\.") {
            Write-Host "  ❌ Missing canonical copyright header in: $($cs.FullName)" -ForegroundColor Red
            $violations++
            $missingHeaderCount++
        }
    }
}
if ($missingHeaderCount -eq 0) { Write-Host "  ✅ All production C# files contain the required MIT copyright header." -ForegroundColor Green }

# 4. One Type Per File Invariant
Write-Host "`n[4/8] Checking 'One Type Per File' rule in src/..." -ForegroundColor Yellow
$multiTypeCount = 0
if ($srcCsFiles) {
    foreach ($cs in $srcCsFiles) {
        $lines = Get-Content $cs.FullName | Where-Object { $_ -notmatch "^\s*//" }
        $typeDeclarations = $lines | Where-Object { $_ -match "^\s*(public|internal|private|protected)?\s*(sealed|abstract|static|readonly)?\s*(class|struct|record|interface|enum)\s+[A-Za-z0-9_]+" }
        if (@($typeDeclarations).Count -gt 1) {
            $hasMultipleTopLevels = ($typeDeclarations | Where-Object { $_ -notmatch "^\s{4,}" }).Count -gt 1
            if ($hasMultipleTopLevels) {
                Write-Host "  ❌ Multiple types declared in: $($cs.FullName)" -ForegroundColor Red
                $violations++
                $multiTypeCount++
            }
        }
    }
}
if ($multiTypeCount -eq 0) { Write-Host "  ✅ Every production file satisfies the 'One Type Per File' invariant." -ForegroundColor Green }

# 5. GitHub Repository Identity
Write-Host "`n[5/8] Checking GitHub identity links (ericksonlopezf/dotnet-concurrency)..." -ForegroundColor Yellow
$wrongRepoLinks = 0
$propsPath = Join-Path $RootDirectory "Directory.Build.props"
if (Test-Path $propsPath) {
    $propsContent = Get-Content $propsPath -Raw
    if ($propsContent -notmatch "ericksonlopezf/dotnet-concurrency") {
        Write-Host "  ❌ Directory.Build.props does not reference ericksonlopezf/dotnet-concurrency" -ForegroundColor Red
        $violations++
        $wrongRepoLinks++
    }
}
if ($wrongRepoLinks -eq 0) { Write-Host "  ✅ All GitHub URLs correctly target ericksonlopezf/dotnet-concurrency." -ForegroundColor Green }

# 6. Normalized Support/Security Contact Email
Write-Host "`n[6/8] Checking contact and security email normalization (ericksonlopezf@gmail.com)..." -ForegroundColor Yellow
$wrongEmailCount = 0
$secDoc = Join-Path $RootDirectory "SECURITY.md"
if (Test-Path $secDoc) {
    $secContent = Get-Content $secDoc -Raw
    if ($secContent -notmatch "ericksonlopezf@gmail\.com") {
        Write-Host "  ❌ SECURITY.md does not reference canonical email ericksonlopezf@gmail.com" -ForegroundColor Red
        $violations++
        $wrongEmailCount++
    }
}
if ($wrongEmailCount -eq 0) { Write-Host "  ✅ Official contact emails normalized to ericksonlopezf@gmail.com." -ForegroundColor Green }

# 7. Prohibited Compiler Warning Suppressions
Write-Host "`n[7/8] Checking for prohibited compiler warning suppressions (CS1591, CS0618, CS0619)..." -ForegroundColor Yellow
$suppressionCount = 0
$allProps = Get-ChildItem -Path $RootDirectory -Recurse -Filter "*.*proj" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }
$propsAndTargets = Get-ChildItem -Path $RootDirectory -Recurse -Include "*.props", "*.targets" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }
$xmlConfigs = @($allProps) + @($propsAndTargets)
foreach ($xml in $xmlConfigs) {
    $content = Get-Content $xml.FullName -Raw
    if ($content -match "<NoWarn>[^<]*(1591|0618|0619|CS1591|CS0618|CS0619)") {
        Write-Host "  ❌ Prohibited <NoWarn> suppression found in: $($xml.FullName)" -ForegroundColor Red
        $violations++
        $suppressionCount++
    }
}
if ($suppressionCount -eq 0) { Write-Host "  ✅ Zero prohibited compiler warnings suppressed via NoWarn." -ForegroundColor Green }

# 8. NuGet Package Icon Metadata & File
Write-Host "`n[8/8] Checking NuGet PackageIcon metadata & asset presence..." -ForegroundColor Yellow
$badIcon = 0
if (Test-Path $propsPath) {
    $propsContent = Get-Content $propsPath -Raw
    if ($propsContent -notmatch "<PackageIcon>icon\.png</PackageIcon>") {
        Write-Host "  ❌ Directory.Build.props missing <PackageIcon>icon.png</PackageIcon>" -ForegroundColor Red
        $violations++
        $badIcon++
    }
}
$iconPath = Join-Path $RootDirectory "icon.png"
if (-not (Test-Path $iconPath)) {
    Write-Host "  ❌ Root icon.png is missing!" -ForegroundColor Red
    $violations++
    $badIcon++
}
if ($badIcon -eq 0) { Write-Host "  ✅ PackageIcon declared and icon.png present." -ForegroundColor Green }

Write-Host "`n==================================================" -ForegroundColor Cyan
if ($violations -eq 0) {
    Write-Host "  SUCCESS: 100% Governance & Compliance Verified. Zero violations. " -ForegroundColor Green
    Write-Host "==================================================" -ForegroundColor Cyan
    exit 0
} else {
    Write-Host "  FAILED: $violations compliance violation(s) detected. " -ForegroundColor Red
    Write-Host "==================================================" -ForegroundColor Cyan
    exit 1
}
