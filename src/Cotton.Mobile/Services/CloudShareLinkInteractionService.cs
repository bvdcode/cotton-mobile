using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace Cotton.Mobile.Services
{
    public class CloudShareLinkInteractionService : ICloudShareLinkInteractionService
    {
        private readonly IClipboard _clipboard;
        private readonly IShare _share;

        public CloudShareLinkInteractionService(IClipboard clipboard, IShare share)
        {
            ArgumentNullException.ThrowIfNull(clipboard);
            ArgumentNullException.ThrowIfNull(share);

            _clipboard = clipboard;
            _share = share;
        }

        public async Task CopyAsync(
            CottonCloudShareLinkSnapshot link,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(link);
            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _clipboard.SetTextAsync(link.ShareUrl);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task ShareAsync(
            CottonCloudShareLinkSnapshot link,
            string title,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(link);
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _share.RequestAsync(
                    new ShareTextRequest
                    {
                        Text = link.ShareUrl,
                        Title = title.Trim(),
                    });
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
