using System.Security.Cryptography;
using System.Text.Json;
using Cotton.Auth;
using Cotton.Files;
using Cotton.Nodes;
using Cotton.Sdk;
using Cotton.Sdk.Auth;

namespace Cotton.Mobile.UploadChecks
{
    internal class CloudUploadCheck : IAsyncDisposable
    {
        private readonly CottonCloudClient _client;
        private readonly Guid _parentId;
        private readonly string _runName;
        private Guid? _runFolderId;

        public CloudUploadCheck(UploadCheckSettings settings, string runName)
        {
            _client = new CottonCloudClient(new InMemoryCottonTokenStore(), new CottonSdkOptions
            {
                BaseAddress = settings.Server,
                DeviceName = "Android upload check",
            });
            _parentId = settings.CloudFolderId;
            _runName = runName;
        }

        public async Task LoginAsync(string accountFile)
        {
            using JsonDocument account = JsonDocument.Parse(await File.ReadAllTextAsync(accountFile));
            string username = account.RootElement.GetProperty("username").GetString()
                ?? throw new InvalidDataException("Account username is missing.");
            string password = account.RootElement.GetProperty("password").GetString()
                ?? throw new InvalidDataException("Account password is missing.");
            await _client.Auth.LoginAsync(new LoginRequestDto { Username = username, Password = password });
            UserDto user = await _client.Auth.MeAsync();
            if (!string.Equals(username, user.Username, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The server returned a different account.");
            }

            _ = await ReadFolderAsync(_parentId);
        }

        public async Task<bool> IsCompleteAsync(int expectedCount)
        {
            NodeDto? folder = (await ReadFolderAsync(_parentId)).Nodes.SingleOrDefault(node => node.Name == _runName);
            if (folder is null)
            {
                return false;
            }

            _runFolderId = folder.Id;
            return (await ReadFolderAsync(folder.Id)).Files.Count == expectedCount;
        }

        public async Task<bool> HasAcceptedFirstChunkAsync(string source)
        {
            Cotton.Settings.ClientSettingsDto settings = await _client.Settings.GetAsync();
            Cotton.Mobile.Services.CottonFileUploadSettings uploadSettings = new(
                settings.MaxChunkSizeBytes, settings.SupportedHashAlgorithm);
            byte[] chunk = new byte[uploadSettings.MaxChunkSizeBytes];
            await using FileStream stream = File.OpenRead(source);
            await stream.ReadExactlyAsync(chunk);
            return await _client.Chunks.ExistsAsync(Convert.ToHexStringLower(SHA256.HashData(chunk)));
        }

        public async Task<Dictionary<string, Guid>> VerifyAsync(string sourceDirectory)
        {
            Guid folderId = _runFolderId ?? throw new InvalidOperationException("Upload folder is missing.");
            NodeContentDto content = await ReadFolderAsync(folderId);
            string[] sources = Directory.GetFiles(sourceDirectory);
            if (content.Nodes.Count != 0 || content.Files.Count != sources.Length)
            {
                throw new InvalidDataException("Unexpected server file or folder count.");
            }

            Dictionary<string, Guid> files = new(StringComparer.Ordinal);
            foreach (string source in sources)
            {
                string name = Path.GetFileName(source);
                NodeFileManifestDto file = content.Files.Single(item => item.Name == name);
                await using MemoryStream downloaded = new();
                await _client.Files.DownloadContentAsync(file.Id, downloaded);
                await using FileStream original = File.OpenRead(source);
                byte[] expectedHash = await SHA256.HashDataAsync(original);
                downloaded.Position = 0;
                byte[] actualHash = await SHA256.HashDataAsync(downloaded);
                if (file.SizeBytes != original.Length || downloaded.Length != original.Length
                    || !expectedHash.SequenceEqual(actualHash))
                {
                    throw new InvalidDataException($"Cloud copy differs: {name}");
                }

                files.Add(name, file.Id);
            }

            return files;
        }

        public async Task CleanupAsync()
        {
            NodeDto? folder = (await ReadFolderAsync(_parentId)).Nodes.SingleOrDefault(node => node.Name == _runName);
            if (folder is not null)
            {
                await _client.Nodes.DeleteAsync(folder.Id, skipTrash: true);
                if ((await ReadFolderAsync(_parentId)).Nodes.Any(node => node.Id == folder.Id))
                {
                    throw new InvalidOperationException("The upload check folder was not removed.");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _client.Auth.LogoutAsync();
            }
            finally
            {
                await _client.DisposeAsync();
            }
        }

        private async Task<NodeContentDto> ReadFolderAsync(Guid folderId)
        {
            CottonPagedResult<NodeContentDto> first = await _client.Nodes.GetChildrenAsync(folderId, page: 1, pageSize: 100);
            for (int page = 2; (page - 1) * 100 < first.TotalCount; page++)
            {
                CottonPagedResult<NodeContentDto> next = await _client.Nodes.GetChildrenAsync(folderId, page, pageSize: 100);
                first.Payload.Nodes.AddRange(next.Payload.Nodes);
                first.Payload.Files.AddRange(next.Payload.Files);
            }

            if (first.Payload.Nodes.Count + first.Payload.Files.Count != first.TotalCount)
            {
                throw new InvalidDataException("Cloud folder changed while reading its contents.");
            }

            return first.Payload;
        }
    }
}
