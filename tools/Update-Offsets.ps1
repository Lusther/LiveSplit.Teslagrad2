<#
.SYNOPSIS
    Generates the GameBuild entry for Teslagrad2Constants.cs from an Il2CppDumper output.

.DESCRIPTION
    Reads the TypeInfo global addresses from Il2CppDumper's script.json, the PE timestamp and
    module size from GameAssembly.dll, and the Steam build id and branch from the app manifest,
    then prints the matching Teslagrad2Constants.Builds entry. With -Apply, adds the entry (or
    replaces the one with the same build id) in Teslagrad2Constants.cs.

    If dump.cs sits next to script.json, the field offsets used by Teslagrad2Memory.cs are
    checked too, so a layout change is caught before testing in game.

.EXAMPLE
    # 1. Dump the build
    Il2CppDumper.exe "<game>\GameAssembly.dll" "<game>\Teslagrad 2_Data\il2cpp_data\Metadata\global-metadata.dat" .\dump
    # 2. Preview, then apply
    .\tools\Update-Offsets.ps1 -DumpDir .\dump -GameDir "<game>"
    .\tools\Update-Offsets.ps1 -DumpDir .\dump -GameDir "<game>" -Apply
#>
param(
    # Il2CppDumper output folder (containing script.json, and optionally dump.cs)
    [Parameter(Mandatory = $true)][string]$DumpDir,
    # Game install folder (containing GameAssembly.dll)
    [Parameter(Mandatory = $true)][string]$GameDir,
    # Steam build id and branch; read from the Steam app manifest when omitted
    [int]$Build,
    [string]$Branch,
    # Defaults to Teslagrad2Constants.cs at the repo root
    [string]$ConstantsPath,
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
if (-not $ConstantsPath) { $ConstantsPath = Join-Path (Split-Path $MyInvocation.MyCommand.Path -Parent) '..\Teslagrad2Constants.cs' }

# GameBuild constructor argument -> IL2CPP class whose TypeInfo global it points to
$BaseClasses = [ordered]@{
    saveDataSlot = 'Game'
    sceneHandler = 'PlayerSceneHandler'
    saveDataFile = 'SaveDataFile'
}

# Field offsets hardcoded in Teslagrad2Memory.cs, as class -> field -> offset
$ExpectedFields = [ordered]@{
    'Game'               = [ordered]@{ activeSlot = 0x10 }
    'SaveDataSlot'       = [ordered]@{
        timeSpent = 0x28
        blinkUnlocked = 0x44; cloakUnlocked = 0x45; waterblinkUnlocked = 0x46; mjolnirUnlocked = 0x47
        powerSlideUnlocked = 0x48; axeUnlocked = 0x49; blinkWireAxeUnlocked = 0x4A; redCloakUnlocked = 0x4B
        omniBlinkUnlocked = 0x4C; doubleJumpUnlocked = 0x4D; secretsMapUnlocked = 0x4E; mapUnlocked = 0x4F
        hulderBossfightBeaten = 0x51; mooseBossFightBeaten = 0x52; fafnirBossFightBeaten = 0x53
        halvtannBossFightBeaten = 0x54; galvanBossFightBeaten = 0x55; trollMiniBossFightBeaten = 0x56
        scrollsPickedUp = 0x80
    }
    'PlayerSceneHandler' = [ordered]@{ _currentScene = 0x0; InElenorBossFight = 0x69 }
    'HyperSceneNode'     = [ordered]@{ scene = 0x28 }
    'SaveDataFile'       = [ordered]@{ instance = 0x0; saveDataSlots = 0x10 }
}

function Get-TypeInfoAddress([string]$json, [string]$className) {
    $pattern = '"Address":\s*(\d+),\s*"Name":\s*"(?:[\w.]+\.)?' + [regex]::Escape($className) + '_TypeInfo"'
    $found = [regex]::Matches($json, $pattern)
    if ($found.Count -ne 1) {
        throw "Expected exactly one '${className}_TypeInfo' in script.json, found $($found.Count)"
    }
    return [int]$found[0].Groups[1].Value
}

# Returns the PE TimeDateStamp and SizeOfImage (= ModuleMemorySize as seen by LiveSplit)
function Get-PeInfo([string]$path) {
    $stream = [IO.File]::OpenRead($path)
    try {
        $reader = New-Object IO.BinaryReader($stream)
        $stream.Position = 0x3C
        $peHeader = $reader.ReadInt32()
        # PE signature (4) + Machine (2) + NumberOfSections (2)
        $stream.Position = $peHeader + 8
        $timestamp = $reader.ReadUInt32()
        # PE signature (4) + COFF header (20) + SizeOfImage offset in optional header (56)
        $stream.Position = $peHeader + 4 + 20 + 56
        return @{ Timestamp = $timestamp; SizeOfImage = $reader.ReadInt32() }
    }
    finally { $stream.Dispose() }
}

# Returns the build id and mounted branch from the Steam app manifest of the game folder
function Get-SteamInfo([string]$gameDir) {
    $installDir = Split-Path $gameDir -Leaf
    $steamApps = Split-Path (Split-Path $gameDir -Parent) -Parent
    foreach ($manifest in Get-ChildItem $steamApps -Filter 'appmanifest_*.acf' -ErrorAction SilentlyContinue) {
        $text = [IO.File]::ReadAllText($manifest.FullName)
        if ($text -notmatch ('"installdir"\s+"' + [regex]::Escape($installDir) + '"')) { continue }
        $info = @{ Build = $null; Branch = 'public' }
        if ($text -match '"buildid"\s+"(\d+)"') { $info.Build = [int]$Matches[1] }
        if ($text -match '"MountedConfig"\s*\{[^}]*"BetaKey"\s+"([^"]*)"') { $info.Branch = $Matches[1] }
        return $info
    }
    return $null
}

function Test-FieldOffsets([string]$dumpCsPath) {
    $lines = [IO.File]::ReadAllLines($dumpCsPath)
    $failures = 0
    foreach ($className in $ExpectedFields.Keys) {
        # Collect the bodies of every top-level class with this name (namespaces may duplicate it)
        $classPattern = '\bclass ' + [regex]::Escape($className) + '( :| //|$)'
        $bodies = @()
        for ($i = 0; $i -lt $lines.Length; $i++) {
            if ($lines[$i] -match $classPattern) {
                $start = $i
                while ($i -lt $lines.Length -and $lines[$i] -ne '}') { $i++ }
                $bodies += , $lines[$start..$i]
            }
        }
        if ($bodies.Count -eq 0) {
            Write-Host "  [MISSING] class $className not found in dump.cs" -ForegroundColor Red
            $failures++
            continue
        }

        foreach ($field in $ExpectedFields[$className].Keys) {
            $expected = $ExpectedFields[$className][$field]
            $fieldPattern = '[ <]' + [regex]::Escape($field) + '(>k__BackingField)?; // 0x([0-9A-F]+)$'
            $actual = $null
            foreach ($body in $bodies) {
                $line = $body | Where-Object { $_ -match $fieldPattern } | Select-Object -First 1
                if ($line -and $line -match $fieldPattern) { $actual = [Convert]::ToInt32($Matches[2], 16); break }
            }
            if ($null -eq $actual) {
                Write-Host ("  [MISSING] {0}.{1} (expected 0x{2:X})" -f $className, $field, $expected) -ForegroundColor Red
                $failures++
            }
            elseif ($actual -ne $expected) {
                Write-Host ("  [MOVED]   {0}.{1}: 0x{2:X} -> 0x{3:X}" -f $className, $field, $expected, $actual) -ForegroundColor Yellow
                $failures++
            }
        }
    }
    return $failures
}

# --- Gather data -------------------------------------------------------------

$scriptJsonPath = Join-Path $DumpDir 'script.json'
$gameAssemblyPath = Join-Path $GameDir 'GameAssembly.dll'
foreach ($path in @($scriptJsonPath, $gameAssemblyPath, $ConstantsPath)) {
    if (-not (Test-Path $path)) { throw "File not found: $path" }
}

Write-Host "Reading $scriptJsonPath ..."
$json = [IO.File]::ReadAllText($scriptJsonPath)
$offsets = [ordered]@{}
foreach ($arg in $BaseClasses.Keys) {
    $offsets[$arg] = Get-TypeInfoAddress $json $BaseClasses[$arg]
}
$json = $null

$pe = Get-PeInfo $gameAssemblyPath
$steam = Get-SteamInfo $GameDir
if (-not $Build -and $steam) { $Build = $steam.Build }
if (-not $Branch -and $steam) { $Branch = $steam.Branch }
if (-not $Build) { throw 'Could not read the Steam build id from the app manifest, pass -Build.' }
if (-not $Branch) { throw 'Could not read the Steam branch from the app manifest, pass -Branch.' }

$offsetArgs = ($offsets.Values | ForEach-Object { '0x{0:X}' -f $_ }) -join ', '
$entry = 'new GameBuild({0}, "{1}", 0x{2:X8}, {3}, {4}),' -f $Build, $Branch, $pe.Timestamp, $pe.SizeOfImage, $offsetArgs

Write-Host ''
Write-Host "  build         $Build"
Write-Host "  branch        $Branch"
Write-Host ('  timestamp     0x{0:X8} ({1:u})' -f $pe.Timestamp, [DateTimeOffset]::FromUnixTimeSeconds($pe.Timestamp).UtcDateTime)
Write-Host "  module size   $($pe.SizeOfImage)"
foreach ($arg in $BaseClasses.Keys) {
    Write-Host ("  {0,-13} {1,-28} 0x{2:X}" -f $arg, "$($BaseClasses[$arg])_TypeInfo", $offsets[$arg])
}

# --- Field offset check ------------------------------------------------------

$dumpCsPath = Join-Path $DumpDir 'dump.cs'
$fieldFailures = 0
if (Test-Path $dumpCsPath) {
    Write-Host ''
    Write-Host 'Checking field offsets used by Teslagrad2Memory.cs ...'
    $fieldFailures = Test-FieldOffsets $dumpCsPath
    if ($fieldFailures -eq 0) { Write-Host '  All field offsets match.' -ForegroundColor Green }
    else { Write-Host "  $fieldFailures field offset(s) need updating in Teslagrad2Memory.cs." -ForegroundColor Yellow }
}
else {
    Write-Warning 'dump.cs not found next to script.json, skipping the field offset check.'
}

# --- Output / apply ----------------------------------------------------------

$constants = [IO.File]::ReadAllText($ConstantsPath)
$existingPattern = '(?m)^([ \t]*)new GameBuild\(' + $Build + ',[^\r\n]*'
$listStartPattern = '(Builds =\r?\n[ \t]*\{\r?\n)([ \t]*)'

$sameTimestamp = [regex]::Match($constants, 'new GameBuild\((\d+), "[^"]*", 0x' + ('{0:X8}' -f $pe.Timestamp) + ',')
if ($sameTimestamp.Success -and $sameTimestamp.Groups[1].Value -ne "$Build") {
    Write-Warning "Timestamp 0x$('{0:X8}' -f $pe.Timestamp) is already used by build $($sameTimestamp.Groups[1].Value)."
}

Write-Host ''
$existing = [regex]::Match($constants, $existingPattern)
if ($existing.Success) {
    $updated = $constants.Remove($existing.Index, $existing.Length).Insert($existing.Index, $existing.Groups[1].Value + $entry)
    $action = "replace the entry for build $Build with"
}
elseif ($constants -match $listStartPattern) {
    $newline = if ($constants.Contains("`r`n")) { "`r`n" } else { "`n" }
    $updated = ([regex]$listStartPattern).Replace($constants, { param($m) $m.Groups[1].Value + $m.Groups[2].Value + $entry + $newline + $m.Groups[2].Value }, 1)
    $action = "add build $Build at the top of Teslagrad2Constants.Builds"
}
else {
    throw "Could not find the Builds list in $ConstantsPath"
}

if ($updated -eq $constants) {
    Write-Host "Build $Build is already up to date." -ForegroundColor Green
}
elseif ($Apply) {
    [IO.File]::WriteAllText((Resolve-Path $ConstantsPath), $updated)
    Write-Host "Done, $action`:" -ForegroundColor Green
    Write-Host "  $entry"
}
else {
    Write-Host "Would $action`:" -ForegroundColor Cyan
    Write-Host "  $entry"
    Write-Host 'Run again with -Apply to write the changes.'
}

if ($fieldFailures -gt 0) { exit 1 }
