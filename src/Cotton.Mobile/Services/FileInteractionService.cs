using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class FileInteractionService : IFileInteractionService
    {
        public async Task OpenAsync(CottonFileDownloadResult file, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            EnsureFileExists(file);

            cancellationToken.ThrowIfCancellationRequested();
            bool opened = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await Launcher.Default.OpenAsync(
                    new OpenFileRequest(file.FileName, new ReadOnlyFile(file.FilePath)));
            });
            cancellationToken.ThrowIfCancellationRequested();
            if (!opened)
            {
                throw new InvalidOperationException("No installed app can open this file.");
            }
        }

        public async Task ShareAsync(CottonFileDownloadResult file, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);
            EnsureFileExists(file);

            cancellationToken.ThrowIfCancellationRequested();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Share.Default.RequestAsync(
                    new ShareFileRequest(file.FileName, new ShareFile(file.FilePath)));
            });
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static void EnsureFileExists(CottonFileDownloadResult file)
        {
            if (!File.Exists(file.FilePath))
            {
                throw new FileNotFoundException("The local file is no longer available.", file.FilePath);
            }
        }
    }
}
