using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace Cotton.Mobile.UploadChecks
{
    internal class AndroidUploadCheck(UploadCheckSettings settings)
    {
        public async Task<string> RunAsync(params string[] arguments)
        {
            ProcessStartInfo start = new("adb")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            start.ArgumentList.Add("-s");
            start.ArgumentList.Add(settings.Serial);
            foreach (string argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }

            using Process process = Process.Start(start)
                ?? throw new InvalidOperationException("ADB did not start.");
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(45));
            Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
            await process.WaitForExitAsync(timeout.Token);
            string result = await output;
            string errors = await error;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"ADB failed: {errors}");
            }

            return result.Trim();
        }

        public async Task StartAsync()
        {
            string activity = (await RunAsync("shell", "cmd", "package", "resolve-activity", "--brief", settings.Package))
                .Split('\n')[^1].Trim();
            if (!activity.StartsWith(settings.Package + "/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The configured application is not installed.");
            }

            await RunAsync("shell", "am", "start", "-W", "-n", activity);
        }

        public async Task RunAllAsync()
        {
            string dump = await RunAsync("shell", "uiautomator", "dump", "/sdcard/cotton-upload-check.xml");
            if (!dump.Contains("dumped to:", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Open the idle Sync screen before starting the upload check.");
            }

            XDocument document = XDocument.Parse(await RunAsync("exec-out", "cat", "/sdcard/cotton-upload-check.xml"));
            XElement button = document.Descendants("node").Single(node =>
                (string?)node.Attribute("content-desc") == "Run all sync folders"
                && (string?)node.Attribute("enabled") == "true");
            string bounds = (string?)button.Attribute("bounds")
                ?? throw new InvalidOperationException("Run all button bounds are missing.");
            int[] coordinates = bounds.Split(['[', ']', ','], StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
            await RunAsync("shell", "input", "tap",
                ((coordinates[0] + coordinates[2]) / 2).ToString(CultureInfo.InvariantCulture),
                ((coordinates[1] + coordinates[3]) / 2).ToString(CultureInfo.InvariantCulture));
        }

        public async Task<string> ReadJournalAsync()
        {
            await RunAsync("shell", "am", "broadcast", "-n",
                settings.Package + "/dev.cottoncloud.mobile.AndroidDiagnosticJournalReceiver",
                "-a", "dev.cottoncloud.app.DUMP_DIAGNOSTICS");
            return await RunAsync("logcat", "-d", "-s", "CottonDiagnostics:I", "*:S", "-v", "raw");
        }

        public async Task StopAsync()
        {
            await RunAsync("shell", "am", "force-stop", settings.Package);
            string processes = await RunAsync("shell", "ps", "-A", "-o", "NAME");
            if (processes.Split('\n').Any(name => name.Trim() == settings.Package))
            {
                throw new InvalidOperationException("The application process did not stop.");
            }
        }
    }
}
