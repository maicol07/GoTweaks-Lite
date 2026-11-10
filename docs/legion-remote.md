# Prepare the Legion Go 2 for SSH

Use a private LAN or VPN. Run the initial setup locally on the Legion from
an elevated PowerShell. Use the IP of the development PC as seen by the
Legion, and keep router port forwarding disabled.

```powershell
$devPcIp = [ipaddress]::Parse((Read-Host 'Private IP of the development PC'))
if ((Get-WindowsCapability -Online -Name 'OpenSSH.Server~~~~0.0.1.0').State -ne 'Installed') {
    Add-WindowsCapability -Online -Name 'OpenSSH.Server~~~~0.0.1.0' -ErrorAction Stop
}
Get-NetFirewallRule -Name OpenSSH-Server-In-TCP -ErrorAction SilentlyContinue |
    Disable-NetFirewallRule -ErrorAction Stop
New-NetFirewallRule -Name GoTweaks-SSH -DisplayName 'SSH from GoTweaks development PC' `
    -Direction Inbound -Action Allow -Protocol TCP -LocalPort 22 -RemoteAddress $devPcIp `
    -ErrorAction Stop
Start-Service sshd
Set-Service sshd -StartupType Automatic
```

Run this setup once. If the development PC's IP changes, update the
`GoTweaks-SSH` rule's remote address. Review any existing inbound SSH rules
if the Legion had OpenSSH Server configured before.

Copy the development PC's **public** key, for example
`$env:USERPROFILE\.ssh\id_ed25519.pub`, to the Legion. Use the Windows
account that will run the widget because MSIX registration is per user.
For a standard account, append the key to that account's
`%USERPROFILE%\.ssh\authorized_keys`. For an administrator account, follow
Microsoft's instructions for
[`administrators_authorized_keys` and its ACL](https://learn.microsoft.com/en-us/windows-server/administration/openssh/openssh_keymanagement).

On the Legion, display the server's ED25519 fingerprint.

```powershell
ssh-keygen -lf "$env:ProgramData\ssh\ssh_host_ed25519_key.pub"
```

From the development PC, compare the first connection prompt with that
fingerprint before accepting it. Confirm the command prints the intended
Windows account and Legion hostname.

```powershell
ssh -o HostKeyAlgorithms=ssh-ed25519 -o PreferredAuthentications=publickey user@legion-private-ip "whoami && hostname"
```

After key login works, set `PasswordAuthentication no` in the global section
of `%ProgramData%\ssh\sshd_config`, before any `Match` block. Validate
with `& "$env:WINDIR\System32\OpenSSH\sshd.exe" -t`, restart `sshd`
locally, then repeat the key login test.

SSH provides a command session. Game Bar, controller and Xbox FSE checks
must run in the signed-in desktop session.
