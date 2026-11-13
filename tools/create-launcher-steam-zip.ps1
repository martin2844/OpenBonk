# Create a private Steam data ZIP for the experimental OpenBonk launcher.
# Requires Windows PowerShell 5.1 or PowerShell 7 and launcher-steam-inputs.json.
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$DataDir,
    [Parameter(Mandatory = $true, Position = 1)][string]$Output,
    [ValidateSet('linux', 'windows')][string]$Platform
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-StreamHash([System.IO.Stream]$stream) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-', '').ToLowerInvariant() }
    finally { $algorithm.Dispose() }
}

$root = [System.IO.Path]::GetFullPath($DataDir).TrimEnd([char[]]@('/', '\'))
$destination = [System.IO.Path]::GetFullPath($Output)
$parent = [System.IO.Path]::GetDirectoryName($destination)
if (-not [System.IO.Directory]::Exists($root)) { throw 'Megabonk_Data directory does not exist.' }
if (-not [System.IO.Directory]::Exists($parent) -or [System.IO.File]::Exists($destination) -or [System.IO.Directory]::Exists($destination)) {
    throw 'Output must be a new file in an existing directory.'
}
$comparison = if ([System.IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
$prefix = $root + [System.IO.Path]::DirectorySeparatorChar
if ($destination.StartsWith($prefix, $comparison)) { throw 'Output must be outside Megabonk_Data.' }

$platformFolders = @{ linux = 'StandaloneLinux64'; windows = 'StandaloneWindows64' }
$present = @($platformFolders.Keys | Where-Object { [System.IO.Directory]::Exists([System.IO.Path]::Combine($root, 'StreamingAssets', 'aa', $platformFolders[$_])) })
if (-not $Platform) {
    if ($present.Count -ne 1) { throw 'Cannot determine platform; specify -Platform linux or windows.' }
    $Platform = $present[0]
}
if ($present -notcontains $Platform) { throw 'Selected platform folder is missing.' }
$manifestPath = Join-Path $PSScriptRoot 'launcher-steam-inputs.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$rows = @($manifest.platforms.$Platform.PSObject.Properties | Sort-Object Name)
if ($rows.Count -ne 39) { throw 'Invalid input inventory.' }
$files = @()
foreach ($row in $rows) {
    $relative = $row.Name
    $parts = $relative.Split('/')
    if ($relative.StartsWith('/') -or ($parts | Where-Object { $_ -eq '' -or $_ -eq '.' -or $_ -eq '..' -or $_.Contains('\') -or $_.Contains(':') })) {
        throw 'Invalid input inventory path.'
    }
    $path = $root
    foreach ($part in $parts) { $path = [System.IO.Path]::Combine($path, $part) }
    if (-not [System.IO.File]::Exists($path)) { throw "Missing file: $relative" }
    $file = [System.IO.FileInfo]::new($path)
    if ($file.Length -ne [long]$row.Value.size) { throw "Unsupported or damaged file: $relative" }
    $inputStream = [System.IO.File]::OpenRead($path)
    try { $hash = Get-StreamHash $inputStream } finally { $inputStream.Dispose() }
    if ($hash -ne $row.Value.sha256) { throw "Unsupported or damaged file: $relative" }
    $files += [PSCustomObject]@{ Relative = $relative; Path = $path; Size = [long]$row.Value.size; Hash = $row.Value.sha256 }
}
Write-Host "Validated $($files.Count) $Platform Steam files."

$temporary = Join-Path $parent ('.openbonk-steam-' + [System.IO.Path]::GetRandomFileName() + '.tmp')
$created = $false
try {
    $fileStream = [System.IO.File]::Open($temporary, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    $created = $true
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($fileStream, [System.IO.Compression.ZipArchiveMode]::Create, $true)
        try {
            foreach ($file in $files) {
                $entry = $archive.CreateEntry('Megabonk_Data/' + $file.Relative, [System.IO.Compression.CompressionLevel]::Optimal)
                $inputStream = [System.IO.File]::OpenRead($file.Path)
                try { $outputStream = $entry.Open(); try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose() } }
                finally { $inputStream.Dispose() }
            }
        } finally { $archive.Dispose() }
        $fileStream.Flush($true)
    } finally { $fileStream.Dispose() }
    if (([System.IO.FileInfo]::new($temporary)).Length -gt 1073741824) { throw 'ZIP exceeds the launcher size limit.' }
    $readStream = [System.IO.File]::OpenRead($temporary)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new($readStream, [System.IO.Compression.ZipArchiveMode]::Read, $true)
        try {
            if ($archive.Entries.Count -ne $files.Count) { throw 'ZIP entry count differs.' }
            foreach ($file in $files) {
                $entry = $archive.GetEntry('Megabonk_Data/' + $file.Relative)
                if ($null -eq $entry -or $entry.Length -ne $file.Size) { throw "ZIP verification failed: $($file.Relative)" }
                $inputStream = $entry.Open()
                try { $hash = Get-StreamHash $inputStream } finally { $inputStream.Dispose() }
                if ($hash -ne $file.Hash) { throw "ZIP verification failed: $($file.Relative)" }
            }
        } finally { $archive.Dispose() }
    } finally { $readStream.Dispose() }
    [System.IO.File]::Move($temporary, $destination)
    $created = $false
    Write-Host "Created $destination. Keep this game-data ZIP private."
} finally {
    if ($created -and [System.IO.File]::Exists($temporary)) { [System.IO.File]::Delete($temporary) }
}
