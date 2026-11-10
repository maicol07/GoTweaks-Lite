<#
    GoTweaks Lite - GUI end-user installer (source for the compiled GoTweaks-Setup.exe).

    This is the GUI counterpart of "Install GoTweaks.ps1" - same steps, same error handling,
    but driven by a small WinForms status window + message boxes instead of a console, and
    compiled to a single double-clickable .exe via ps2exe (see Build-Release.ps1).

    Elevation is handled by the compiled exe's own manifest (ps2exe -requireAdmin), so this
    script does NOT self-relaunch like the console version - Windows shows the UAC prompt
    before any of this code runs at all.

    The .msixbundle and .cer are EMBEDDED in the compiled exe (ps2exe -embedFiles, see
    Build-Release.ps1) and extracted to %TEMP%\GoTweaksSetup\ at startup, so the whole install
    is a single downloaded file - nothing needs to sit next to the exe. The -BundlePath/
    -CertPath params below are a manual override (dev convenience: running the raw .ps1, or a
    rebuild without -embedFiles); if omitted, the script looks for the extracted copies first,
    then falls back to searching next to the exe.

    What it does (and why it needs admin):
      1. Imports the bundled .cer into the machine's Trusted People + Trusted Root stores -
         required for Windows to accept a self-signed MSIX.
      2. Closes Game Bar / the GoTweaks helper if running (they hold the old package open
         and would cause error 0x80073D02).
      3. Installs / updates the package for the current user.
      4. Offers the embedded USB/IP and HidHide installers for VIIPER.

    No data is sent anywhere. Everything runs locally.
#>

[CmdletBinding()]
param(
    # Optional explicit paths; auto-detected (embedded copy, then the exe's own folder) if omitted.
    [string]$BundlePath,
    [string]$CertPath
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# --- Status window ----------------------------------------------------
$form = New-Object System.Windows.Forms.Form
$form.Text = "GoTweaks Lite Setup"
$form.Size = New-Object System.Drawing.Size(420, 140)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false
$form.MinimizeBox = $false
$form.TopMost = $true

$label = New-Object System.Windows.Forms.Label
$label.Text = "Starting..."
$label.AutoSize = $false
$label.Size = New-Object System.Drawing.Size(380, 30)
$label.Location = New-Object System.Drawing.Point(16, 16)
$form.Controls.Add($label)

$progress = New-Object System.Windows.Forms.ProgressBar
$progress.Style = 'Marquee'
$progress.MarqueeAnimationSpeed = 30
$progress.Size = New-Object System.Drawing.Size(380, 20)
$progress.Location = New-Object System.Drawing.Point(16, 56)
$form.Controls.Add($progress)

$form.Show()
$form.Refresh()

function Set-Status([string]$msg) {
    $label.Text = $msg
    $form.Refresh()
    [System.Windows.Forms.Application]::DoEvents()
}

function Fail([string]$msg) {
    $form.Close()
    [System.Windows.Forms.MessageBox]::Show($msg, "GoTweaks Lite Setup - Error", `
        [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    exit 1
}

function Test-UsbIpInstalled {
    if (-not (Get-Service -Name usbip2_ude -ErrorAction SilentlyContinue)) { return $false }
    foreach ($directory in $env:ProgramFiles, ${env:ProgramFiles(x86)}) {
        if (-not $directory) { continue }
        $client = Join-Path $directory 'USBip\usbip.exe'
        if (-not (Test-Path -LiteralPath $client -PathType Leaf)) { continue }
        $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($client)
        if ($version.FileMajorPart -eq 0 -and $version.FileMinorPart -eq 9 -and
            $version.FileBuildPart -eq 8 -and $version.FilePrivatePart -eq 1) { return $true }
    }
    return $false
}

function Install-EmbeddedDriver([string]$file, [string]$sha256, [string[]]$arguments) {
    # The extracted files are in the user's temp directory. Verify the copy in a protected directory before running it elevated.
    $directory = New-Item -ItemType Directory -Path (Join-Path $env:ProgramFiles ("GoTweaksSetup-" + [guid]::NewGuid().ToString('N')))
    $payload = Join-Path $directory.FullName $file
    try {
        Copy-Item -LiteralPath (Join-Path $embeddedDir $file) -Destination $payload
        if ((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash -ne $sha256 -or
            (Get-AuthenticodeSignature -LiteralPath $payload).Status -ne 'Valid') {
            throw "The embedded installer failed verification: $file."
        }
        $form.Hide()
        $process = Start-Process -FilePath $payload -ArgumentList $arguments -PassThru -Wait -WindowStyle Normal
        return $process.ExitCode
    } finally {
        $form.Show()
        Remove-Item -LiteralPath $payload -Force -ErrorAction SilentlyContinue
        if (-not (Get-ChildItem -LiteralPath $directory.FullName -Force)) {
            Remove-Item -LiteralPath $directory.FullName -ErrorAction SilentlyContinue
        }
    }
}

function Select-Drivers([bool]$usbIpInstalled, [bool]$hidHideInstalled) {
    $dialog = New-Object System.Windows.Forms.Form
    $dialog.Text = 'Optional VIIPER drivers'
    $dialog.ClientSize = New-Object System.Drawing.Size(450, 172)
    $dialog.StartPosition = 'CenterParent'
    $dialog.FormBorderStyle = 'FixedDialog'
    $dialog.MaximizeBox = $false
    $dialog.MinimizeBox = $false

    $usb = New-Object System.Windows.Forms.CheckBox
    $usb.AutoSize = $true
    $usb.Location = New-Object System.Drawing.Point(16, 20)
    $usb.Text = if ($usbIpInstalled) { 'USB/IP 0.9.8.1 is already installed' } else { 'Install USB/IP 0.9.8.1 (required for VIIPER)' }
    $usb.Enabled = !$usbIpInstalled
    $dialog.Controls.Add($usb)

    $hid = New-Object System.Windows.Forms.CheckBox
    $hid.AutoSize = $true
    $hid.Location = New-Object System.Drawing.Point(16, 55)
    $hid.Text = if ($hidHideInstalled) { 'HidHide is already installed' } else { 'Install HidHide (hide the physical controller)' }
    $hid.Enabled = !$hidHideInstalled
    $dialog.Controls.Add($hid)

    $next = New-Object System.Windows.Forms.Button
    $next.Text = 'Continue'
    $next.Location = New-Object System.Drawing.Point(340, 116)
    $next.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $dialog.AcceptButton = $next
    $dialog.Controls.Add($next)

    try {
        if ($dialog.ShowDialog($form) -eq [System.Windows.Forms.DialogResult]::OK) {
            return @{ UsbIp = $usb.Checked; HidHide = $hid.Checked }
        }
        return @{ UsbIp = $false; HidHide = $false }
    } finally { $dialog.Dispose() }
}

# --- Locate the package + certificate ----------------------------------
# The exe's own folder, regardless of how ps2exe resolves $PSScriptRoot internally.
$here = Split-Path -Parent ([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
# Where ps2exe -embedFiles extracts the embedded bundle/cert at startup (must match the
# target paths given to -embedFiles in Build-Release.ps1).
$embeddedDir = Join-Path $env:TEMP "GoTweaksSetup"

Set-Status "Locating the package..."

if (-not $BundlePath) {
    $embeddedBundle = Join-Path $embeddedDir "GoTweaks.msixbundle"
    if (Test-Path $embeddedBundle) {
        $BundlePath = $embeddedBundle
    } else {
        # Dev convenience: same folder as the exe, or a raw uncompiled run of this script.
        $BundlePath = Get-ChildItem -Path $here -Filter *.msixbundle -ErrorAction SilentlyContinue |
                      Sort-Object LastWriteTime | Select-Object -Last 1 -ExpandProperty FullName
        if (-not $BundlePath) {
            $appPkgs = Join-Path $here '..\XboxGamingBarPackage\AppPackages'
            if (Test-Path $appPkgs) {
                $BundlePath = Get-ChildItem -Path $appPkgs -Recurse -Filter *.msixbundle -ErrorAction SilentlyContinue |
                              Sort-Object LastWriteTime | Select-Object -Last 1 -ExpandProperty FullName
            }
        }
    }
}
if (-not $BundlePath -or -not (Test-Path $BundlePath)) {
    Fail "No .msixbundle found (embedded copy missing, and none next to Setup.exe either)."
}

if (-not $CertPath) {
    $embeddedCert = Join-Path $embeddedDir "GoTweaks.cer"
    if (Test-Path $embeddedCert) {
        $CertPath = $embeddedCert
    } else {
        $bundleDir = Split-Path -Parent $BundlePath
        $CertPath = Get-ChildItem -Path $bundleDir -Filter *.cer -ErrorAction SilentlyContinue |
                    Select-Object -First 1 -ExpandProperty FullName
        if (-not $CertPath) {
            $CertPath = Get-ChildItem -Path $here -Filter *.cer -ErrorAction SilentlyContinue |
                        Select-Object -First 1 -ExpandProperty FullName
        }
    }
}
if (-not $CertPath -or -not (Test-Path $CertPath)) {
    Fail "No .cer certificate found (embedded copy missing, and none next to the package either)."
}

# --- 1. Trust the signing certificate -----------------------------------
Set-Status "Trusting the signing certificate..."
try {
    Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
    Import-Certificate -FilePath $CertPath -CertStoreLocation Cert:\LocalMachine\Root        | Out-Null
} catch {
    Fail "Failed to import the certificate: $($_.Exception.Message)"
}

# --- 2. Close anything holding the old package open ----------------------
Set-Status "Closing Game Bar / GoTweaks helper if running..."
try { schtasks.exe /End /TN "GoTweaks\GoTweaksHelper" 2>&1 | Out-Null } catch {}
Get-Process XboxGamingBarHelper, GameBar, GameBarFTServer, GameBarPresenceWriter `
    -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# --- 3. Install / update the package --------------------------------------
Set-Status "Installing GoTweaks Lite..."
function Install-Bundle { Add-AppxPackage -Path $BundlePath -ForceUpdateFromAnyVersion -ErrorAction Stop }

try {
    Install-Bundle
} catch {
    if ("$_" -match '0x80073D02') {
        Set-Status "Package is in use - closing blockers again and retrying..."
        Get-Process XboxGamingBarHelper, GameBar, GameBarFTServer, GameBarPresenceWriter `
            -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 5
        try { Install-Bundle }
        catch { Fail "Install failed even after retry. Close the Game Bar overlay (or sign out and back in) and run this again.`n$($_.Exception.Message)" }
    }
    elseif ("$_" -match '0x800B0109') {
        Fail "Windows still does not trust the certificate (0x800B0109). Try running the installer again."
    }
    else {
        Fail "Install failed: $($_.Exception.Message)"
    }
}

$driverErrors = @()
$restartRequired = $false
if ((Test-Path -LiteralPath (Join-Path $embeddedDir 'USBip-0.9.8.1-x64.exe')) -and
    (Test-Path -LiteralPath (Join-Path $embeddedDir 'HidHide_1.5.230_x64.exe'))) {
    $usbIpInstalled = Test-UsbIpInstalled
    $hidHideInstalled = [bool](Get-Service -Name HidHide -ErrorAction SilentlyContinue)
    if (!$usbIpInstalled -or !$hidHideInstalled) {
        $selected = Select-Drivers $usbIpInstalled $hidHideInstalled
        if ($selected.UsbIp) {
            try {
                Set-Status 'Installing USB/IP...'
                $code = Install-EmbeddedDriver 'USBip-0.9.8.1-x64.exe' '38CAD6D4432B52D5BB9409D9AD03B72FDFFC4ADA4CD3A48FBECA1A2752A8518A' @('/NORESTART', '/RESTARTEXITCODE=3010')
                if ($code -eq 3010 -or $code -eq 1641) { $restartRequired = $true }
                elseif ($code -ne 0 -or !(Test-UsbIpInstalled)) { throw "USB/IP installation did not complete (exit code $code)." }
            } catch { $driverErrors += $_.Exception.Message }
        }
        if ($selected.HidHide) {
            try {
                Set-Status 'Installing HidHide...'
                $code = Install-EmbeddedDriver 'HidHide_1.5.230_x64.exe' 'F4BBBCB82E6258641B887C74BC81C4C5F66E4AA811808DFC304347687B7605F6' @('/norestart')
                if ($code -eq 3010 -or $code -eq 1641) { $restartRequired = $true }
                elseif ($code -ne 0 -or !(Get-Service -Name HidHide -ErrorAction SilentlyContinue)) { throw "HidHide installation did not complete (exit code $code)." }
                else { $restartRequired = $true }
            } catch { $driverErrors += $_.Exception.Message }
        }
    }
}

# Clean up the extracted embedded copies - only relevant when they came from $embeddedDir,
# harmless no-op otherwise (Remove-Item on a path that isn't there / isn't ours).
if ($BundlePath -like "$embeddedDir\*") {
    try { Remove-Item $embeddedDir -Recurse -Force -ErrorAction SilentlyContinue } catch {}
}

$form.Close()
$message = "GoTweaks Lite installed.`n`nOpen the Game Bar (Win + G), click the widget menu, and add GoTweaks."
if ($restartRequired) { $message += "`n`nRestart Windows before using the installed drivers." }
if ($driverErrors.Count) { $message += "`n`n" + ($driverErrors -join "`n") }
$icon = if ($driverErrors.Count) { [System.Windows.Forms.MessageBoxIcon]::Warning } else { [System.Windows.Forms.MessageBoxIcon]::Information }
[System.Windows.Forms.MessageBox]::Show(
    $message,
    "GoTweaks Lite Setup",
    [System.Windows.Forms.MessageBoxButtons]::OK,
    $icon) | Out-Null
