using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XboxGamingBarHelper.ControllerEmulation.Viiper
{
    /// <summary>
    /// Thin wrapper over usbip-win2's usbip.exe, used to explicitly attach libviiper's
    /// exported devices to the local UDE bus (and detach them on teardown).
    ///
    /// WHY THIS EXISTS: libviiper's viiper_device_add is *documented* to auto-attach the
    /// device to the local UDE driver, but on some usbip-win2 UDE installs that internal
    /// attach silently no-ops — viiper_device_add returns success and the USBIP server
    /// exports the device on 127.0.0.1:3241, yet no child ever appears under the UDE host
    /// controller and "usbip.exe port" stays empty, so no gamepad reaches Windows.
    /// Driving the standard usbip client against libviiper's own loopback server lands it.
    ///
    /// Verified on-device 2026-07-08 (LeGo2, usbip-win2 0.9.7.8): the internal auto-attach
    /// produced nothing (empty UDE host-controller child list, empty "usbip.exe port"), but
    ///   usbip.exe -t 3241 attach -r 127.0.0.1 -b 1-1
    /// plugged in the emulated DualSense and gamepads worked. This class reproduces that
    /// call after every device add. It is idempotent (devices already attached are skipped),
    /// so on machines where the internal auto-attach *does* work it is a harmless no-op
    /// rather than a duplicate attachment.
    /// </summary>
    internal static class UsbipCli
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Must match ViiperService.Initialize's listen address.
        private const string Host = "127.0.0.1";
        private const int Port = 3241;
        private static readonly Regex ViiperImport = new Regex(
            $@"^\s*->\s*usbip://(?:{Regex.Escape(Host)}|localhost):{Port}/(\d+-\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly string[] ExePaths =
        {
            @"C:\Program Files\USBip\usbip.exe",
            @"C:\Program Files (x86)\USBip\usbip.exe",
        };

        internal static string ResolveExe()
        {
            foreach (var p in ExePaths)
            {
                if (!File.Exists(p)) continue;
                var version = FileVersionInfo.GetVersionInfo(p);
                if (version.FileMajorPart == 0 && version.FileMinorPart == 9 &&
                    version.FileBuildPart == 8 && version.FilePrivatePart == 1)
                    return p;
                Logger.Warn("VIIPER requires the usbip-win2 0.9.8.1 client.");
            }
            return null;
        }

        /// <summary>
        /// Attaches every device libviiper is exporting on its loopback USBIP server that
        /// isn't already imported into the local UDE bus. Best-effort: logs and returns
        /// quietly if usbip.exe is missing or the CLI misbehaves.
        /// </summary>
        public static bool EnsureAttached(uint bus, uint device)
        {
            string exe = ResolveExe();
            if (exe == null)
            {
                Logger.Warn("usbip.exe not found; cannot attach VIIPER device to the UDE bus.");
                return false;
            }

            string busId = $"{bus}-{device}";
            string output = Run(exe, $"-t {Port} port");
            if (output == null)
                return false;
            if (ReadViiperPorts(output).Any(import => import.Value == busId))
                return true;

            Run(exe, $"-t {Port} attach -r {Host} -b {busId}");
            return ReadViiperPorts(Run(exe, $"-t {Port} port")).Any(import => import.Value == busId);
        }

        /// <summary>Detaches every UDE port imported from libviiper's loopback server. Best-effort.</summary>
        public static bool DetachAll()
        {
            string exe = ResolveExe();
            if (exe == null) return false;

            string output = Run(exe, $"-t {Port} port");
            if (output == null) return false;

            bool detached = true;
            foreach (var import in ReadViiperPorts(output))
            {
                if (Run(exe, $"detach -p {import.Key}") != null)
                    Logger.Info($"Detached VIIPER USB/IP port {import.Key}.");
                else
                    detached = false;
            }
            return detached;
        }

        internal static IEnumerable<KeyValuePair<string, string>> ReadViiperPorts(string output)
        {
            if (string.IsNullOrEmpty(output)) yield break;

            string port = null;
            foreach (var line in output.Split('\n'))
            {
                var header = Regex.Match(line, @"^\s*Port\s+(\d+)\s*:");
                if (header.Success)
                    port = header.Groups[1].Value;

                var import = ViiperImport.Match(line);
                if (port != null && import.Success)
                    yield return new KeyValuePair<string, string>(port, import.Groups[1].Value);
            }
        }

        private static string Run(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using (var proc = Process.Start(psi))
                {
                    if (proc == null) return null;

                    // Read both streams concurrently, not sequentially - reading stdout to
                    // completion before even starting to drain stderr is the classic .NET
                    // Process deadlock: if usbip.exe writes enough to the unread stream to
                    // fill its OS pipe buffer, it blocks on that write forever, so it never
                    // closes stdout, so ReadToEnd() here never returns either. That hung this
                    // call indefinitely on a misbehaving usbip.exe, which meant the helper's
                    // ProcessExit handler (Environment.Exit -> viiperEmulationManager.Stop() ->
                    // DetachAll() -> here) never completed - the helper process became a
                    // permanent zombie still holding the single-instance mutex and the
                    // scheduled task's "running" state (MultipleInstances=IgnoreNew), so every
                    // later RunTaskNow() silently no-opped until a full reboot or reinstall
                    // reset that state. Reading both streams via Task avoids the deadlock, and
                    // killing the process on timeout stops it becoming a zombie in the first
                    // place (also keeps stray usbip.exe instances from piling up - see the
                    // VIIPER/usbip-win2 native-state theory in the helper-CLR-crash notes).
                    var stdoutTask = proc.StandardOutput.ReadToEndAsync();
                    var stderrTask = proc.StandardError.ReadToEndAsync();
                    bool exited = proc.WaitForExit(15000);

                    if (!exited)
                    {
                        Logger.Warn($"usbip {args} did not exit within 15s - killing it.");
                        try { proc.Kill(); } catch (Exception killEx) { Logger.Debug($"usbip kill failed: {killEx.Message}"); }
                    }

                    // Killing (or a normal exit) closes the pipes, so these resolve promptly;
                    // still bound the wait so a pathological case can't hang the caller.
                    Task.WaitAll(new Task[] { stdoutTask, stderrTask }, 3000);
                    string stdout = stdoutTask.Status == TaskStatus.RanToCompletion ? stdoutTask.Result : string.Empty;
                    string stderr = stderrTask.Status == TaskStatus.RanToCompletion ? stderrTask.Result : string.Empty;
                    if (!exited || proc.ExitCode != 0)
                    {
                        Logger.Warn($"usbip command failed ({args}): {stderr}");
                        return null;
                    }
                    return (stdout ?? string.Empty) + "\n" + (stderr ?? string.Empty);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, $"usbip exec failed ({args})");
                return null;
            }
        }
    }
}
