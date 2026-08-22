// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    internal static class CottonDiagnosticCategoryPolicy
    {
        private static readonly Dictionary<string, LogLevel> MinimumLevels =
            new(StringComparer.Ordinal)
            {
                ["Cotton.Mobile.Services.CottonSessionService"] = LogLevel.Information,
                ["Cotton.Mobile.Services.SecureStorageCottonTokenStore"] = LogLevel.Information,
                ["Cotton.Mobile.Services.SyncExecutionWorkflow"] = LogLevel.Information,
                ["Cotton.Mobile.Services.CottonAutomaticSyncRunner"] = LogLevel.Information,
                ["Cotton.Mobile.Services.CottonDeviceToCloudSyncCoordinator"] = LogLevel.Information,
                ["Cotton.Mobile.Services.CottonUploadOnlySyncPlanExecutor"] = LogLevel.Information,
                ["Cotton.Mobile.Platforms.Android.AndroidAutomaticSyncExecutor"] = LogLevel.Information,
                ["Cotton.Mobile.Platforms.Android.AndroidPeriodicSyncWorker"] = LogLevel.Information,
                ["Cotton.Mobile.Platforms.Android.AndroidSyncRootWorker"] = LogLevel.Information,
                ["Cotton.Sdk.Internal.CottonHttpTransport"] = LogLevel.Warning,
            };

        public static bool IsEnabled(string category, LogLevel level)
        {
            return MinimumLevels.TryGetValue(category, out LogLevel minimumLevel)
                && level >= minimumLevel;
        }
    }
}
