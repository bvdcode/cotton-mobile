// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using System.Diagnostics;
using Android.App;
using Android.App.Job;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    [Service(Name = ProbeComponentName, Permission = "android.permission.BIND_JOB_SERVICE", Exported = false)]
    public class AndroidStopProbeJobService : AndroidMediaStoreSyncJobService
    {
        private const string ProbeComponentName = "dev.cottoncloud.mobile.AndroidStopProbeJobService";
        private const int JobId = 1129598210;

        public static void Configure(bool cancel)
        {
            Context context = global::Android.App.Application.Context;
            JobScheduler scheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService)!;
            scheduler.Cancel(JobId);
            if (cancel)
            {
                return;
            }

            ComponentName component = new(context, ProbeComponentName);
            using JobInfo.Builder builder = new(JobId, component);
            using JobInfo job = builder.SetMinimumLatency(60_000)!.Build()!;
            if (scheduler.Schedule(job) != JobScheduler.ResultSuccess)
            {
                throw new InvalidOperationException("Stop probe scheduling failed.");
            }
        }

        public override bool OnStartJob(JobParameters? @params)
        {
            ILoggerFactory factory = IPlatformApplication.Current!.Services.GetRequiredService<ILoggerFactory>();
            factory.AddProvider(new AndroidSlowStopLogger(2206));
            _ = global::Android.Util.Log.Info(AndroidCancellationProbeRunner.LogTag, "native-job-started");
            return true;
        }

        public override bool OnStopJob(JobParameters? @params)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool reschedule = base.OnStopJob(@params);
            _ = global::Android.Util.Log.Info(AndroidCancellationProbeRunner.LogTag,
                $"native-stop-returned:milliseconds={stopwatch.ElapsedMilliseconds}:main={MainThread.IsMainThread}");
            return reschedule;
        }
    }
}
#endif
