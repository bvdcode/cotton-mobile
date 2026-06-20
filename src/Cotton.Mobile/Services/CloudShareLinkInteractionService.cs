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
            await CopyAsync([link], cancellationToken);
        }

        public async Task CopyAsync(
            IReadOnlyList<CottonCloudShareLinkSnapshot> links,
            CancellationToken cancellationToken = default)
        {
            string text = CreateLinkText(links);
            await CopyTextAsync(text, cancellationToken);
        }

        public async Task ShareAsync(
            CottonCloudShareLinkSnapshot link,
            string title,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(link);
            await ShareAsync([link], title, cancellationToken);
        }

        public async Task ShareAsync(
            IReadOnlyList<CottonCloudShareLinkSnapshot> links,
            string title,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            string text = CreateLinkText(links);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _share.RequestAsync(
                    new ShareTextRequest
                    {
                        Text = text,
                        Title = title.Trim(),
                    });
            });
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task CopyTextAsync(string text, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _clipboard.SetTextAsync(text);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static string CreateLinkText(IReadOnlyList<CottonCloudShareLinkSnapshot> links)
        {
            ArgumentNullException.ThrowIfNull(links);

            CottonCloudShareLinkSnapshot[] normalizedLinks = links
                .Where(link => link is not null)
                .ToArray();
            if (normalizedLinks.Length == 0)
            {
                throw new ArgumentException("At least one share link is required.", nameof(links));
            }

            return string.Join(Environment.NewLine, normalizedLinks.Select(link => link.ShareUrl));
        }
    }
}
