using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cotton.Mobile.UploadChecks
{
    internal static partial class UploadCheckRunner
    {
        private const int LargeFileSize = 64 * 1024 * 1024;
        private const int FileCount = 17;

        public static async Task RunAsync(string[] arguments)
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentException("Usage: upload-check <configuration.json>");
            }

            UploadCheckSettings settings = JsonSerializer.Deserialize<UploadCheckSettings>(
                await File.ReadAllTextAsync(arguments[0]), JsonSerializerOptions.Web)
                ?? throw new InvalidDataException("Upload check configuration is missing.");
            Validate(settings);
            string runName = "upload-check-" + Guid.NewGuid().ToString("N");
            string output = Path.GetFullPath(Path.Combine(settings.OutputDirectory, runName));
            string sources = Path.Combine(output, "sources");
            Directory.CreateDirectory(sources);
            await CreateSourcesAsync(sources);
            string remoteSources = settings.AndroidSourceDirectory + "/" + runName;
            AndroidUploadCheck android = new(settings);
            await using CloudUploadCheck cloud = new(settings, runName);
            await cloud.LoginAsync(settings.AccountFile);
            string baselineJournal = await android.ReadJournalAsync();
            await android.StopAsync();
            DateTimeOffset started = DateTimeOffset.UtcNow;
            bool interrupted = false;
            try
            {
                await android.RunAsync("push", sources + "/.", remoteSources);
                await android.StartAsync();
                Console.WriteLine("Waiting for the automatic startup upload of isolated sources.");
                DateTime deadline = DateTime.UtcNow.AddMinutes(10);
                while (DateTime.UtcNow < deadline)
                {
                    string journal = await android.ReadJournalAsync();
                    await File.WriteAllTextAsync(Path.Combine(output, "diagnostics.log"), journal);
                    if (!interrupted && HasNewLargeUpload(journal, baselineJournal)
                        && await cloud.HasAcceptedFirstChunkAsync(Path.Combine(sources, "large.bin")))
                    {
                        await android.StopAsync();
                        if (await cloud.IsCompleteAsync(FileCount))
                        {
                            throw new InvalidOperationException("The upload finished before interruption; rerun on a slower connection.");
                        }

                        interrupted = true;
                        Console.WriteLine("Stopped during the large upload. Restarting the application.");
                        await android.StartAsync();
                    }

                    if (interrupted && await cloud.IsCompleteAsync(FileCount))
                    {
                        Dictionary<string, Guid> first = await cloud.VerifyAsync(sources);
                        await VerifyOriginalsAsync(android, sources, remoteSources);
                        await RepeatAsync(android, output);
                        Dictionary<string, Guid> repeated = await cloud.VerifyAsync(sources);
                        if (first.Any(file => repeated[file.Key] != file.Value))
                        {
                            throw new InvalidDataException("A repeated run replaced a cloud file.");
                        }

                        await File.WriteAllTextAsync(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new
                        {
                            started,
                            completed = DateTimeOffset.UtcNow,
                            verifiedFiles = first.Count,
                            interrupted,
                            acceptedChunkBeforeInterruption = true,
                            retainedFileIds = true,
                            originalsPreserved = true,
                        }, JsonSerializerOptions.Web));
                        Console.WriteLine($"Passed: {first.Count} exact cloud copies, process recovery, and repeat without duplicates.");
                        return;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2));
                }

                throw new TimeoutException("Android upload and recovery did not finish within ten minutes.");
            }
            finally
            {
                await android.StopAsync();
                await android.RunAsync("shell", "rm", "-rf", remoteSources);
                string remaining = await android.RunAsync("shell", "ls", "-1", settings.AndroidSourceDirectory);
                if (remaining.Split('\n').Any(name => name.Trim() == runName))
                {
                    throw new IOException("The generated Android folder was not removed.");
                }
                await cloud.CleanupAsync();
                Console.WriteLine("Removed the generated Android and cloud folders; retained local results.");
            }
        }

        private static bool HasNewLargeUpload(string journal, string baseline)
        {
            string size = "started with " + LargeFileSize.ToString(CultureInfo.InvariantCulture) + " bytes.";
            return journal.Split('\n').Any(line => line.Contains(size, StringComparison.Ordinal)
                && !baseline.Contains(line, StringComparison.Ordinal));
        }

        private static async Task RepeatAsync(AndroidUploadCheck android, string output)
        {
            string baseline = await android.ReadJournalAsync();
            await android.RunAllAsync();
            DateTime deadline = DateTime.UtcNow.AddMinutes(2);
            while (DateTime.UtcNow < deadline)
            {
                string journal = await android.ReadJournalAsync();
                if (journal.Split('\n').Any(line => line.Contains("Manual sync-all completed for", StringComparison.Ordinal)
                    && !baseline.Contains(line, StringComparison.Ordinal)))
                {
                    await File.WriteAllTextAsync(Path.Combine(output, "repeated-diagnostics.log"), journal);
                    return;
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            throw new TimeoutException("The repeated manual run did not finish.");
        }

        private static async Task VerifyOriginalsAsync(AndroidUploadCheck android, string sources, string remoteSources)
        {
            foreach (string source in Directory.GetFiles(sources))
            {
                await using FileStream original = File.OpenRead(source);
                string expected = Convert.ToHexStringLower(await SHA256.HashDataAsync(original));
                string result = await android.RunAsync("shell", "sha256sum", remoteSources + "/" + Path.GetFileName(source));
                if (!result.StartsWith(expected + " ", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("An Android original was removed or changed.");
                }
            }
        }

        private static async Task CreateSourcesAsync(string directory)
        {
            for (int index = 0; index < FileCount - 1; index++)
            {
                await File.WriteAllBytesAsync(Path.Combine(directory, $"file-{index:D2}.bin"),
                    RandomNumberGenerator.GetBytes(index * 1024));
            }

            await using FileStream large = File.Create(Path.Combine(directory, "large.bin"));
            for (int index = 0; index < LargeFileSize / (1024 * 1024); index++)
            {
                await large.WriteAsync(RandomNumberGenerator.GetBytes(1024 * 1024));
            }
        }

        private static void Validate(UploadCheckSettings settings)
        {
            if (!EmulatorSerial().IsMatch(settings.Serial)
                || settings.Package is not ("dev.cottoncloud.app" or "dev.cottoncloud.app.debug"))
            {
                throw new ArgumentException("An explicit Android emulator and Cotton package are required.");
            }

            if (!SourcePath().IsMatch(settings.AndroidSourceDirectory)
                || settings.CloudFolderId == Guid.Empty || settings.Server.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Use HTTPS, a cloud folder ID, and an isolated /sdcard/Documents/<folder> source.");
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(settings.OutputDirectory);
            ArgumentException.ThrowIfNullOrWhiteSpace(settings.AccountFile);
        }

        [GeneratedRegex(@"^emulator-[0-9]+$")]
        private static partial Regex EmulatorSerial();

        [GeneratedRegex(@"^/sdcard/Documents/[A-Za-z0-9_-]+$")]
        private static partial Regex SourcePath();
    }
}
