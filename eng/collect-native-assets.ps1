param(
    [Parameter(Mandatory)] [string] $Rid,
    [Parameter(Mandatory)] [string] $Triplet,
    [Parameter(Mandatory)] [string] $InstallRoot,
    [Parameter(Mandatory)] [string] $OutputRoot
)

$ErrorActionPreference = 'Stop'
$source = Join-Path $InstallRoot $Triplet
$destination = Join-Path $OutputRoot $Rid
New-Item -ItemType Directory -Path $destination -Force | Out-Null

if ($Rid.StartsWith('win-', [StringComparison]::Ordinal)) {
    $assets = Get-ChildItem -LiteralPath (Join-Path $source 'bin') -Filter '*.dll' -File
}
elseif ($Rid.StartsWith('linux-', [StringComparison]::Ordinal)) {
    $assets = Get-ChildItem -LiteralPath (Join-Path $source 'lib') -File |
        Where-Object { $_.Name -match '\.so(\.|$)' }
}
elseif ($Rid.StartsWith('osx-', [StringComparison]::Ordinal)) {
    $assets = Get-ChildItem -LiteralPath (Join-Path $source 'lib') -Filter '*.dylib' -File
}
else {
    throw "Unsupported RID: $Rid"
}

if (-not ($assets | Where-Object { $_.Name -match 'tesseract' })) {
    throw "No Tesseract shared library was produced for $Rid."
}

$assets | Copy-Item -Destination $destination -Force
$notices = Join-Path $destination 'licenses'
New-Item -ItemType Directory -Path $notices -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $source 'share') -Filter 'copyright' -File -Recurse |
    ForEach-Object {
        $port = Split-Path $_.DirectoryName -Leaf
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $notices "$port.txt") -Force
    }

Write-Output "Collected $($assets.Count) native libraries for $Rid."
