#if ANDROID
using Android.App;
using Android.Content;

namespace Cotton.Mobile.Services
{
    public interface IAndroidDocumentScanActivityResultBridge
    {
        Task<Intent?> StartScanAsync(
            Activity activity,
            IntentSender intentSender,
            CancellationToken cancellationToken = default);

        bool TryHandleActivityResult(int requestCode, Result resultCode, Intent? data);
    }
}
#endif
