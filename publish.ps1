<#
.SYNOPSIS
  Builds and packages FileNoteManager for distribution (USB / hand-off).

.DESCRIPTION
  Creates a ready-to-use folder under dist\ containing:
    FileNoteManager.exe            — main application (single-file)
    FileNoteManager.Shell.*.dll/json — shell extension (tooltip + context menu)
    all dependency DLLs            — SQLite, Dapper, etc.
    README.txt                     — quick-start guide

  Requires .NET 8 SDK installed on the BUILD machine.
  Target machines need .NET 8 Desktop Runtime (download link in README.txt).
  For a fully self-contained build (no .NET 8 needed on target), add -SelfContained.

.EXAMPLE
  # Framework-dependent (~15 MB, needs .NET 8 on target):
  .\publish.ps1

  # Self-contained (~120 MB, runs on any Windows 10/11 x64):
  .\publish.ps1 -SelfContained
#>
param(
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$root     = $PSScriptRoot
$uiProj   = Join-Path $root "src\FileNoteManager.UI\FileNoteManager.UI.csproj"
$distDir  = Join-Path $root "dist"

# ── Clean output ──────────────────────────────────────────────────────────────
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
New-Item $distDir -ItemType Directory | Out-Null

# ── Build arguments ───────────────────────────────────────────────────────────
$args = @(
    "publish", $uiProj,
    "-c", "Release",
    "-r", "win-x64",
    "-p:PublishProfile=win-x64",
    "-o", $distDir
)
if ($SelfContained) {
    $args += "-p:SelfContained=true"
} else {
    $args += "--no-self-contained"
}

Write-Host "`n==> Publishing FileNoteManager..." -ForegroundColor Cyan
& dotnet @args
if ($LASTEXITCODE -ne 0) { throw "Publish failed (exit $LASTEXITCODE)" }

# ── Copy shell companion files (comhost needs them next to itself) ────────────
$shellBin = Join-Path $root "src\FileNoteManager.Shell\bin\Release\net8.0-windows"
foreach ($f in @("FileNoteManager.Shell.runtimeconfig.json",
                  "FileNoteManager.Shell.deps.json",
                  "FileNoteManager.Shell.comhost.dll")) {
    $src = Join-Path $shellBin $f
    if (Test-Path $src) {
        Copy-Item $src $distDir -Force
        Write-Host "  Copied $f"
    }
}

# ── Write README ──────────────────────────────────────────────────────────────
$selfContainedNote = if ($SelfContained) {
    "This package is self-contained — no .NET runtime installation required."
} else {
    "Requires .NET 8 Desktop Runtime on the target machine.
Download: https://dotnet.microsoft.com/download/dotnet/8.0
(Choose '.NET Desktop Runtime 8.x' for Windows x64)"
}

@"
==============================================================
  File Note Manager
==============================================================

QUICK START
-----------
1. Copy this entire folder to any location (USB drive, desktop, etc.).
2. Run FileNoteManager.exe.
   - On first launch it registers the right-click context menu
     and the hover tooltip handler automatically.
3. Right-click any file or folder in Explorer → "编辑文件备注".

TOOLTIP
-------
After registering, hover over any file or folder that has a note
to see the note text in the Explorer tooltip.
If the tooltip does not appear immediately, restart Explorer:
  Task Manager → Details → explorer.exe → End Task → File → Run → explorer.exe

PORTABILITY
-----------
- All data is stored in:  %APPDATA%\FileNoteManager\fnm.db
- Registry entries are written to HKCU (current user only, no admin needed).
- To unregister, open File Note Manager → Settings → Unregister Shell.

$selfContainedNote

==============================================================
"@ | Set-Content (Join-Path $distDir "README.txt") -Encoding UTF8

# ── Summary ───────────────────────────────────────────────────────────────────
$files = Get-ChildItem $distDir -File
$totalMB = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host "`n==> Done!  Output: $distDir" -ForegroundColor Green
Write-Host "    $($files.Count) files, ${totalMB} MB total"
Write-Host ""
