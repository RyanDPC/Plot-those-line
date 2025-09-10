@echo off
setlocal ENABLEDELAYEDEXPANSION

REM Determine script directory and repo root
set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"

REM Paths
set "INPUT_MD=%REPO_ROOT%\docs\Livrables\rapport.md"
set "OUTPUT_PDF=%REPO_ROOT%\docs\Livrables\rapport.pdf"
set "HEADER_TEX=%REPO_ROOT%\docs\pandoc\header.tex"
set "RESOURCE_PATH=%REPO_ROOT%"

REM Check pandoc availability
where pandoc >nul 2>nul
if errorlevel 1 (
  echo [ERREUR] Pandoc introuvable. Installez-le et ajoutez-le au PATH: https://pandoc.org/installing.html
  exit /b 1
)

REM Validate files
if not exist "%INPUT_MD%" (
  echo [ERREUR] Fichier Markdown introuvable: %INPUT_MD%
  exit /b 1
)
if not exist "%HEADER_TEX%" (
  echo [ERREUR] Header LaTeX introuvable: %HEADER_TEX%
  exit /b 1
)

REM Ensure output directory exists
for %%D in ("%OUTPUT_PDF%") do set "OUT_DIR=%%~dpD"
if not exist "%OUT_DIR%" (
  mkdir "%OUT_DIR%" >nul 2>nul
)

echo Generation du PDF...
echo Entree : %INPUT_MD%
echo Sortie : %OUTPUT_PDF%
echo Header : %HEADER_TEX%

pandoc "%INPUT_MD%" -o "%OUTPUT_PDF%" --from markdown+yaml_metadata_block --pdf-engine=xelatex --include-in-header="%HEADER_TEX%" --resource-path="%RESOURCE_PATH%"
if errorlevel 1 (
  echo [ERREUR] Pandoc a renvoye un code d'erreur.
  exit /b 1
)

echo PDF genere avec succes.
exit /b 0
