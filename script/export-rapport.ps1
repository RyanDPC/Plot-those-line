# Requires: Pandoc + TeX engine (XeLaTeX recommended)
# Usage examples:
#   powershell -ExecutionPolicy Bypass -File script/export-rapport.ps1
#   powershell -ExecutionPolicy Bypass -File script/export-rapport.ps1 -Input docs/Livrables/rapport.md -Output docs/Livrables/rapport.pdf

[CmdletBinding()] param(
    [string]$Input,
    [string]$Output
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Script and repo paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir

# Default paths
$defaultInput  = Join-Path $repoRoot 'docs/Livrables/rapport.md'
$defaultOutput = Join-Path $repoRoot 'docs/Livrables/rapport.pdf'
$headerTex     = Join-Path $repoRoot 'docs/pandoc/header.tex'
$resourcePath  = $repoRoot

$inputMd   = if ($Input) { Resolve-Path -LiteralPath $Input } else { $defaultInput }
$outputPdf = if ($Output) { if (-not (Test-Path (Split-Path -Parent $Output))) { New-Item -ItemType Directory -Path (Split-Path -Parent $Output) | Out-Null }; Resolve-Path -LiteralPath (Split-Path -Parent $Output) | Out-Null; $Output } else { $defaultOutput }

# Validate dependencies
$pandoc = Get-Command pandoc -ErrorAction SilentlyContinue
if (-not $pandoc) {
    Write-Error 'Pandoc introuvable. Installez Pandoc et ajoutez-le au PATH: https://pandoc.org/installing.html'
}

# Validate files
if (-not (Test-Path -LiteralPath $inputMd)) {
    Write-Error "Fichier Markdown introuvable: $inputMd"
}
if (-not (Test-Path -LiteralPath $headerTex)) {
    Write-Error "Header LaTeX introuvable: $headerTex"
}

# Ensure output directory exists
$outDir = Split-Path -Parent $outputPdf
if (-not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
}

# Build pandoc arguments
$pandocArgs = @(
    ('"' + $inputMd + '"'),
    '-o', ('"' + $outputPdf + '"'),
    '--from', 'markdown+yaml_metadata_block',
    '--pdf-engine=xelatex',
    '--include-in-header', ('"' + $headerTex + '"'),
    '--resource-path', ('"' + $resourcePath + '"')
)

Write-Host 'Génération du PDF…' -ForegroundColor Cyan
Write-Host ("Entrée : {0}" -f $inputMd)
Write-Host ("Sortie : {0}" -f $outputPdf)
Write-Host ("Header : {0}" -f $headerTex)

# Execute pandoc
& $pandoc.Source $pandocArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error ("Pandoc a renvoyé un code d'erreur: {0}" -f $LASTEXITCODE)
}

Write-Host 'PDF généré avec succès.' -ForegroundColor Green
