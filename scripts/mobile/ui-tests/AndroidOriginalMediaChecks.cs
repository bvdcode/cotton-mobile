// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID && DEBUG
using Cotton.Mobile.Services;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidOriginalMediaChecks
    {
        private const string LogTag = "CottonUploadUiTests";

        public static async Task ShowPromptAsync(IServiceProvider services)
        {
            try
            {
                bool accepted = await services.GetRequiredService<MediaOriginalRestoreReviewHandler>()
                    .ConfirmAsync(300, CancellationToken.None);
                _ = global::Android.Util.Log.Info(LogTag, $"original-media-prompt:accepted={accepted}");
            }
            catch (Exception exception)
            {
                _ = global::Android.Util.Log.Error(LogTag, $"original-media-prompt:failed:{exception}");
            }
        }

        public static async Task CheckCancellationAsync(IServiceProvider services)
        {
            string path = Path.Combine(FileSystem.CacheDirectory, "original-media-review-check.bin");
            try
            {
                await using CottonForegroundCancellation lifetime = new(
                    services.GetRequiredService<IApplicationForegroundService>(), CancellationToken.None);
                try
                {
                    await Task.Run(async () =>
                    {
                        await using FileStream content = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                        content.SetLength(32 * 1024 * 1024);
                        _ = global::Android.Util.Log.Info(LogTag, "original-media-review:hash-started");
                        for (int index = 0; index < 300; index++)
                        {
                            content.Position = 0;
                            _ = await CottonContentHash.ComputeSha256Async(content, lifetime.Token);
                        }

                        throw new InvalidOperationException("The review was not cancelled while hashing.");
                    }, lifetime.Token);
                }
                catch (OperationCanceledException) when (lifetime.Token.IsCancellationRequested)
                {
                    _ = global::Android.Util.Log.Info(LogTag, "original-media-review:stopped");
                }
            }
            catch (Exception exception)
            {
                _ = global::Android.Util.Log.Error(LogTag, $"original-media-review:failed:{exception}");
            }
            finally
            {
                File.Delete(path);
            }
        }

    }
}
#endif
