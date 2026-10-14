// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Android.App.Job;
using Android.Content;
using Android.Util;
using AndroidX.Work;

namespace Cotton.Mobile.Platforms.Android
{
    internal static class AndroidCancellationProbeJob
    {
        private const string JobNamespace = "androidx.work.systemjobscheduler";
        private const string WorkIdExtra = "EXTRA_WORK_SPEC_ID";

        public static void RecordScheduled(OneTimeWorkRequest request)
        {
            string workId = request.Id?.ToString()
                ?? throw new InvalidOperationException("Test work ID is unavailable.");
            Context context = global::Android.App.Application.Context;
            JobScheduler scheduler = context.GetSystemService(Context.JobSchedulerService) as JobScheduler
                ?? throw new InvalidOperationException("JobScheduler is unavailable.");
            string jobNamespace = string.Empty;
            if (OperatingSystem.IsAndroidVersionAtLeast(34))
            {
                scheduler = scheduler.ForNamespace(JobNamespace);
                jobNamespace = JobNamespace;
            }

            JobInfo job = scheduler.AllPendingJobs?.Single(candidate => candidate.Extras.GetString(WorkIdExtra) == workId)
                ?? throw new InvalidOperationException("Scheduled test job is unavailable.");
            _ = Log.Info(AndroidCancellationProbeRunner.LogTag,
                $"job-scheduled:namespace={jobNamespace}:id={job.Id}");
        }
    }
}
#endif
