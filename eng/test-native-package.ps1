param(
    [Parameter(Mandatory)] [string] $PackageDirectory,
    [Parameter(Mandatory)] [string] $Rid
)

$ErrorActionPreference = 'Stop'
$packageName = "TesseractRuntime.Native.$Rid.0.1.0-preview.1.nupkg"
$packagePath = Join-Path $PackageDirectory $packageName
if (-not (Test-Path -LiteralPath $packagePath)) { throw "Missing package: $packageName" }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $packagePath))
try {
    $prefix = "runtimes/$Rid/native/"
    $entries = @($archive.Entries.FullName)
    $nativeFiles = @($entries | Where-Object {
        $_.StartsWith($prefix, [StringComparison]::Ordinal) -and
        $_ -match '\.(dll|dylib)$|\.so(\.|$)'
    })
    $notices = @($entries | Where-Object {
        $_.StartsWith("${prefix}licenses/", [StringComparison]::Ordinal) -and $_.EndsWith('.txt')
    })

    if (-not ($entries -contains 'README.md')) { throw "$packageName does not contain README.md." }
    if (-not ($nativeFiles | Where-Object { $_ -match 'tesseract' })) {
        throw "$packageName does not contain the Tesseract runtime library."
    }
    if (-not ($nativeFiles | Where-Object { $_ -match 'lept(onica)?' })) {
        throw "$packageName does not contain the Leptonica runtime library."
    }
    if ($notices.Count -lt 2) { throw "$packageName does not contain dependency license notices." }
    if ($entries | Where-Object { $_ -match '\.(pdb|exp|lib|a|cs)$' }) {
        throw "$packageName leaks build-time files."
    }
    if ($entries | Where-Object {
        $_.StartsWith('runtimes/', [StringComparison]::Ordinal) -and
        -not $_.StartsWith($prefix, [StringComparison]::Ordinal)
    }) {
        throw "$packageName contains assets for a different runtime identifier."
    }
}
finally {
    $archive.Dispose()
}

Write-Output "Native package structure passed for $Rid."
