#if ANDROID
using Android.App;
using Android.Content;

namespace Cotton.Mobile.Services
{
    public interface IAndroidDocumentTreeActivityResultBridge
    {
        Task<Intent?> StartOpenDocumentTreeAsync(
            Activity activity,
            Intent intent,
            CancellationToken cancellationToken = default);

        bool TryHandleActivityResult(int requestCode, Result resultCode, Intent? data);
    }
}
#endif
