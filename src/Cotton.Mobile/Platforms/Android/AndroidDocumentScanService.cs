#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Gms.Extensions;
using Microsoft.Maui.ApplicationModel;
using Net.Google.MLKit.Vision.DocumentScanner;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentScanService : IDocumentScanService
    {
        private const int MaxPageCount = 20;

        private readonly IAndroidDocumentScanActivityResultBridge _activityResultBridge;

        public AndroidDocumentScanService(IAndroidDocumentScanActivityResultBridge activityResultBridge)
        {
            ArgumentNullException.ThrowIfNull(activityResultBridge);

            _activityResultBridge = activityResultBridge;
        }

        public bool IsAvailable => true;

        public async Task<CottonFileUploadSource?> ScanDocumentAsync(CancellationToken cancellationToken = default)
        {
            Activity activity = Platform.CurrentActivity
                ?? throw new InvalidOperationException("Document scanner needs an active Android activity.");
            ContentResolver contentResolver = activity.ContentResolver
                ?? throw new InvalidOperationException("Document scanner needs an active Android content resolver.");

            GmsDocumentScannerOptions options = new GmsDocumentScannerOptions.Builder()
                .SetScannerMode(GmsDocumentScannerOptions.ScannerModeFull)
                .SetGalleryImportAllowed(true)
                .SetPageLimit(MaxPageCount)
                .SetResultFormats(GmsDocumentScannerOptions.ResultFormatPdf)
                .Build();
            IGmsDocumentScanner scanner = GmsDocumentScanning.GetClient(options);

            IntentSender intentSender = await MainThread.InvokeOnMainThreadAsync(async () =>
                    await scanner.GetStartScanIntent(activity).AsAsync<IntentSender>())
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            Intent? resultIntent = await MainThread.InvokeOnMainThreadAsync(() =>
                    _activityResultBridge.StartScanAsync(activity, intentSender, cancellationToken))
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (resultIntent is null)
            {
                return null;
            }

            GmsDocumentScanningResult? result = GmsDocumentScanningResult.FromActivityResultIntent(resultIntent);
            GmsDocumentScanningResult.Pdf? pdf = result?.GetPdf();
            if (pdf is null || pdf.Uri is null)
            {
                throw new InvalidOperationException("Document scanner did not return a PDF.");
            }

            Android.Net.Uri pdfUri = pdf.Uri;
            var snapshot = new CottonFileUploadSourceSnapshot(
                CottonScannedDocumentUploadContract.CreatePdfFileName(DateTimeOffset.Now),
                CottonScannedDocumentUploadContract.PdfContentType,
                TryGetContentSize(contentResolver, pdfUri),
                CottonScannedDocumentUploadContract.CreateMetadata(NormalizePageCount(pdf.PageCount)));
            return new CottonFileUploadSource(
                snapshot,
                token => OpenPdfStreamAsync(contentResolver, pdfUri, token));
        }

        private static int? NormalizePageCount(int pageCount)
        {
            return pageCount > 0 ? pageCount : null;
        }

        private static long? TryGetContentSize(ContentResolver contentResolver, Android.Net.Uri uri)
        {
            using AssetFileDescriptor? descriptor = contentResolver.OpenAssetFileDescriptor(uri, "r");
            long length = descriptor?.Length ?? -1;
            return length >= 0 ? length : null;
        }

        private static Task<Stream> OpenPdfStreamAsync(
            ContentResolver contentResolver,
            Android.Net.Uri uri,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stream? stream = contentResolver.OpenInputStream(uri);
            if (stream is null)
            {
                throw new InvalidOperationException("Could not open scanned document PDF.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(stream);
        }
    }
}
#endif
