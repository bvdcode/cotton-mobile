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

            cancellationToken.ThrowIfCancellationRequested();
            await MainThread.InvokeOnMainThreadAsync(
                () => Launcher.Default.OpenAsync(
                    new OpenFileRequest(file.FileName, new ReadOnlyFile(file.FilePath))));
        }

        public async Task ShareAsync(CottonFileDownloadResult file, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(file);

            cancellationToken.ThrowIfCancellationRequested();
            await MainThread.InvokeOnMainThreadAsync(
                () => Share.Default.RequestAsync(
                    new ShareFileRequest(file.FileName, new ShareFile(file.FilePath))));
        }
    }
}
