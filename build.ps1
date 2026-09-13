param([switch]$Package)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$projectPath = Join-Path $projectRoot 'DadsDecor.csproj'
$packageRoot = Join-Path $projectRoot 'package'
$dllPath = Join-Path $projectRoot 'bin\Release\net48\DadsDecor.dll'
$manifestPath = Join-Path $packageRoot 'manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

dotnet build $projectPath -c Release
if ($LASTEXITCODE -ne 0) { throw "DadsDecor build exited with code $LASTEXITCODE" }

$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($dllPath).Version
$assemblySemVer = "$($assemblyVersion.Major).$($assemblyVersion.Minor).$($assemblyVersion.Build)"
if ($assemblySemVer -ne $manifest.version_number) {
    throw "Assembly version $assemblySemVer does not match manifest version $($manifest.version_number)."
}

if ($Package) {
    $entries = [ordered]@{
        'DadsDecor.dll' = $dllPath
        'manifest.json' = $manifestPath
        'README.md' = (Join-Path $packageRoot 'README.md')
        'icon.png' = (Join-Path $packageRoot 'icon.png')
        'CHANGELOG.md' = (Join-Path $projectRoot 'CHANGELOG.md')
        'LICENSE' = (Join-Path $projectRoot 'LICENSE')
    }
    foreach ($source in $entries.Values) {
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            throw "Required package file is missing: $source"
        }
    }

    Add-Type -AssemblyName System.Drawing
    $icon = [System.Drawing.Image]::FromFile($entries['icon.png'])
    try {
        if ($icon.Width -ne 256 -or $icon.Height -ne 256) {
            throw "Thunderstore icon must be 256x256; found $($icon.Width)x$($icon.Height)."
        }
        if ($icon.RawFormat.Guid -ne [System.Drawing.Imaging.ImageFormat]::Png.Guid) {
            throw 'Thunderstore icon must be PNG.'
        }
    }
    finally {
        $icon.Dispose()
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $distRoot = Join-Path $projectRoot 'dist'
    $archiveRoot = Join-Path $projectRoot 'Archive\package-builds'
    New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $archiveRoot -Force | Out-Null

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    foreach ($artifact in Get-ChildItem -LiteralPath $distRoot -Force) {
        $archiveName = if ($artifact.PSIsContainer) {
            "$($artifact.Name)-$stamp"
        }
        else {
            "$($artifact.BaseName)-$stamp$($artifact.Extension)"
        }
        Move-Item -LiteralPath $artifact.FullName -Destination (Join-Path $archiveRoot $archiveName)
    }

    $folderPath = Join-Path $distRoot "DadsDecor-$($manifest.version_number)"
    $zipPath = Join-Path $distRoot "DadsDecor-$($manifest.version_number).zip"
    New-Item -ItemType Directory -Path $folderPath | Out-Null
    foreach ($entry in $entries.GetEnumerator()) {
        Copy-Item -LiteralPath $entry.Value -Destination (Join-Path $folderPath $entry.Key)
    }

    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $entries.GetEnumerator()) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $zip,
                $entry.Value,
                $entry.Key,
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $zip.Dispose()
    }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $entryNames = @($zip.Entries | ForEach-Object FullName)
        foreach ($required in $entries.Keys) {
            if ($required -notin $entryNames) { throw "ZIP is missing root entry: $required" }
        }
        if ($entryNames | Where-Object { $_ -match '[/\\]' }) { throw 'ZIP contains a nested directory.' }
    }
    finally {
        $zip.Dispose()
    }

    Write-Output "Thunderstore package: $zipPath"
    Write-Output "SHA256: $((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash)"
}
