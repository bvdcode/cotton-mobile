#if ANDROID
using Android.Content;
using Android.Util;
using AndroidX.Work;
using WorkNetworkType = AndroidX.Work.NetworkType;

namespace Cotton.Mobile.Services
{
    public class AndroidBackgroundSyncHost : ICottonAndroidBackgroundSyncHost
    {
        private const string LogTag = "CottonSync";

        public Task<CottonAndroidBackgroundSyncScheduleResult> ScheduleAsync(
            CottonAndroidBackgroundSyncRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            Context context = Android.App.Application.Context;
            WorkManager workManager = WorkManager.GetInstance(context)
                ?? throw new InvalidOperationException("Android WorkManager is unavailable.");

            OneTimeWorkRequest workRequest = CreateWorkManagerRequest(request);
            ExistingWorkPolicy keepExisting = ExistingWorkPolicy.Keep
                ?? throw new InvalidOperationException("Android WorkManager KEEP policy is unavailable.");
            _ = workManager.EnqueueUniqueWork(
                request.ScheduleIdentity.UniqueWorkName,
                keepExisting,
                workRequest);

            Log.Info(
                LogTag,
                $"Scheduled Android WorkManager sync request {request.ScheduleIdentity.UniqueWorkName} for {request.EligibleRootCount} roots.");
            return Task.FromResult(CottonAndroidBackgroundSyncScheduleResult.Scheduled(
                request,
                $"Scheduled Android background sync for {request.EligibleRootCount} folders."));
        }

        private static OneTimeWorkRequest CreateWorkManagerRequest(CottonAndroidBackgroundSyncRequest request)
        {
            Java.Lang.Class workerClass = Java.Lang.Class.FromType(typeof(AndroidWorkManagerSyncWorker))
                ?? throw new InvalidOperationException("Android WorkManager sync worker type is unavailable.");

            return new OneTimeWorkRequest.Builder(workerClass)
                .SetInputData(AndroidBackgroundSyncWorkData.Create(request))
                .SetConstraints(CreateWorkManagerConstraints())
                .AddTag(request.ScheduleIdentity.SyncTag)
                .Build()
                ?? throw new InvalidOperationException("Android WorkManager sync request builder returned no request.");
        }

        private static Constraints CreateWorkManagerConstraints()
        {
            var builder = new Constraints.Builder();
            builder.SetRequiredNetworkType(
                WorkNetworkType.Connected
                    ?? throw new InvalidOperationException("Android WorkManager connected network type is unavailable."));

            return builder.Build()
                ?? throw new InvalidOperationException("Android WorkManager sync constraints builder returned no constraints.");
        }
    }
}
#endif
