using Android.Content;

namespace Cotton.Mobile.Services
{
    public interface IAndroidShareIntentStagingService
    {
        Task<CottonShareIntakeSnapshot?> StageAsync(
            Intent intent,
            ContentResolver? contentResolver,
            CancellationToken cancellationToken = default);
    }
}
