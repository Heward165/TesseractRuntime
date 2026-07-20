param([Parameter(Mandatory)] [string] $PackageDirectory)

$ErrorActionPreference = 'Stop'
$expected = @(
    'TesseractRuntime.0.1.0-preview.1.nupkg',
    'TesseractRuntime.ImageSharp.0.1.0-preview.1.nupkg',
    'TesseractRuntime.SkiaSharp.0.1.0-preview.1.nupkg'
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($name in $expected) {
    $path = Join-Path $PackageDirectory $name
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing package: $name" }

    $archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $path))
    try {
        $entries = @($archive.Entries.FullName)
        if (-not ($entries -contains 'README.md')) { throw "$name does not contain README.md." }
        if (-not ($entries | Where-Object { $_ -match '^lib/net8\.0/.+\.dll$' })) { throw "$name is missing net8.0." }
        if (-not ($entries | Where-Object { $_ -match '^lib/net10\.0/.+\.dll$' })) { throw "$name is missing net10.0." }
        if ($entries | Where-Object { $_ -match '\.(pdb|cs)$' }) { throw "$name leaks build files." }
    }
    finally {
        $archive.Dispose()
    }

    $symbols = [IO.Path]::ChangeExtension($path, '.snupkg')
    if (-not (Test-Path -LiteralPath $symbols)) { throw "Missing symbols package for $name." }
}

Write-Output 'Managed package structure passed.'
