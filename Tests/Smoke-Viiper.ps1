#Requires -Version 5.1
param(
    [Parameter(Mandatory)]
    [string]$HelperDirectory
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -ne 'Desktop' -or ![Environment]::Is64BitProcess) {
    throw 'Run this smoke test in x64 Windows PowerShell.'
}

$directory = (Resolve-Path -LiteralPath $HelperDirectory).Path
$temporary = New-Item -ItemType Directory -Path (Join-Path $env:TEMP ('GoTweaksViiperSmoke-' + [guid]::NewGuid()))
$harness = Join-Path $temporary.FullName 'ViiperSmoke.exe'
try {
    Add-Type -OutputAssembly $harness -OutputType ConsoleApplication -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

public static class ViiperSmoke
{
    public static void Main(string[] args)
    {
        var directory = args[0];
        Environment.SetEnvironmentVariable("PATH", directory + ";" + Environment.GetEnvironmentVariable("PATH"));
        var assembly = Assembly.LoadFrom(Path.Combine(directory, "XboxGamingBarHelper.exe"));
        var type = assembly.GetType("XboxGamingBarHelper.ControllerEmulation.Viiper.ViiperService", true);
        var cli = assembly.GetType("XboxGamingBarHelper.ControllerEmulation.Viiper.UsbipCli", true);
        var readPorts = cli.GetMethod("ReadViiperPorts", BindingFlags.Static | BindingFlags.NonPublic);
        var ports = (IEnumerable<KeyValuePair<string, string>>)readPorts.Invoke(null, new object[] {
            "Port 01: device in use\r\n -> usbip://localhost:3241/1-1\r\n" +
            "Port 02: device in use\r\n -> usbip://127.0.0.1:3241/1-1\r\n" +
            "Port 03: device in use\r\n -> usbip://localhost:3240/1-1\r\n" +
            "Port 04: device in use\r\n -> usbip://192.168.1.2:3241/1-1\r\n"
        });
        if (!ports.Select(port => port.Key + "/" + port.Value).SequenceEqual(new[] { "01/1-1", "02/1-1" }))
            throw new InvalidOperationException("USB/IP parsing must match both loopback aliases and ignore unrelated imports.");

        for (int cycle = 1; cycle <= 2; cycle++)
        {
            var service = Activator.CreateInstance(type, true);
            using ((IDisposable)service)
            {
                if (!(bool)type.GetMethod("Initialize").Invoke(service, new object[] { "127.0.0.1:0" }) ||
                    !(bool)type.GetMethod("CreateBus").Invoke(service, new object[] { 1u }))
                    throw new InvalidOperationException("VIIPER initialization failed on cycle " + cycle);

                var types = (string[])type.GetMethod("GetDeviceTypes").Invoke(service, null);
                foreach (var device in new[] { "xbox360", "dualshock4", "dualsense", "switchpro", "steamcontroller" })
                    if (!types.Contains(device))
                        throw new InvalidOperationException("VIIPER did not enumerate " + device);

                if (!(bool)type.GetMethod("RemoveBus").Invoke(service, new object[] { 1u }))
                    throw new InvalidOperationException("VIIPER failed to remove the bus on cycle " + cycle);
            }
        }
        Console.WriteLine("VIIPER managed smoke test passed (2 loopback cycles).");
    }
}
'@
    Copy-Item -LiteralPath (Join-Path $directory 'XboxGamingBarHelper.exe.config') -Destination "$harness.config"
    & $harness $directory
    if ($LASTEXITCODE -ne 0) { throw "VIIPER smoke test failed with exit code $LASTEXITCODE." }
} finally {
    foreach ($file in @($harness, "$harness.config", (Join-Path $temporary.FullName 'viiper_go_debug.log'))) {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file }
    }
    [IO.Directory]::Delete($temporary.FullName)
}
