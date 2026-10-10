param([switch]$Package)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'DadsHunting.csproj'
$dll = Join-Path $root 'bin\Release\net48\DadsHunting.dll'
$manifestPath = Join-Path $root 'package\manifest.json'

dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "DadsHunting build exited with code $LASTEXITCODE" }

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$version = [Reflection.AssemblyName]::GetAssemblyName($dll).Version
$assemblyVersion = "$($version.Major).$($version.Minor).$($version.Build)"
if ($assemblyVersion -ne $manifest.version_number) {
    throw "Assembly version $assemblyVersion differs from manifest version $($manifest.version_number)."
}
if (-not $Package) { return }

$entries = [ordered]@{
    'DadsHunting.dll' = $dll
    'manifest.json' = $manifestPath
    'README.md' = (Join-Path $root 'package\README.md')
    'icon.png' = (Join-Path $root 'package\icon.png')
    'CHANGELOG.md' = (Join-Path $root 'CHANGELOG.md')
    'LICENSE' = (Join-Path $root 'LICENSE')
    'SOURCES.md' = (Join-Path $root 'SOURCES.md')
}
foreach ($source in $entries.Values) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing package file: $source" }
}

Add-Type -AssemblyName System.Drawing
$icon = [Drawing.Image]::FromFile($entries['icon.png'])
try {
    if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
        throw "Thunderstore icon must be 256x256; found $($icon.Width)x$($icon.Height)."
    }
    if ($icon.RawFormat.Guid -ne [Drawing.Imaging.ImageFormat]::Png.Guid) {
        throw 'Thunderstore icon must be PNG.'
    }
}
finally { $icon.Dispose() }

$dist = Join-Path $root 'dist'
$archive = Join-Path $root 'Archive\package-builds'
$rootFull = [IO.Path]::GetFullPath($root).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
foreach ($path in @($dist, $archive)) {
    $full = [IO.Path]::GetFullPath($path)
    if (-not $full.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Package path is outside the project: $full"
    }
}
New-Item -ItemType Directory -Path $dist,$archive -Force | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
foreach ($item in Get-ChildItem -LiteralPath $dist -Filter 'DadsHunting-*') {
    $archivedName = if ($item.PSIsContainer) { "$($item.Name)-$stamp" } else { "$($item.BaseName)-$stamp$($item.Extension)" }
    Move-Item -LiteralPath $item.FullName -Destination (Join-Path $archive $archivedName)
}

$folder = Join-Path $dist "DadsHunting-$($manifest.version_number)"
$zip = Join-Path $dist "DadsHunting-$($manifest.version_number).zip"
New-Item -ItemType Directory -Path $folder | Out-Null
foreach ($entry in $entries.GetEnumerator()) {
    Copy-Item -LiteralPath $entry.Value -Destination (Join-Path $folder $entry.Key)
}
Compress-Archive -Path (Join-Path $folder '*') -DestinationPath $zip

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archiveFile = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $names = @($archiveFile.Entries | ForEach-Object FullName)
    foreach ($name in $entries.Keys) {
        if ($name -notin $names) { throw "ZIP is missing $name" }
    }
    if ($names | Where-Object { $_ -match '[/\\]' }) { throw 'ZIP has a nested directory.' }
}
finally { $archiveFile.Dispose() }

Write-Host "Package: $zip"
Write-Host "SHA256: $((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash)"
