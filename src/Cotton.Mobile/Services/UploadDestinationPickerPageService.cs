// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class UploadDestinationPickerPageService : IUploadDestinationPickerPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public UploadDestinationPickerPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public async Task<CottonUploadDestinationSnapshot?> PickAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<CottonUploadDestinationSnapshot?>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var viewModel = ActivatorUtilities.CreateInstance<CaptureDestinationPickerViewModel>(
                    _serviceProvider,
                    instanceUri,
                    (Func<CottonUploadDestinationSnapshot, Task<bool>>)(destination =>
                    {
                        completion.TrySetResult(destination);
                        return Task.FromResult(true);
                    }),
                    "No folder selected.",
                    (Func<CottonUploadDestinationSnapshot, string>)(destination =>
                        $"Destination set to {destination.Path}."));
                var page = ActivatorUtilities.CreateInstance<CaptureDestinationPickerPage>(
                    _serviceProvider,
                    viewModel);
                page.Disappearing += (_, _) =>
                {
                    completion.TrySetResult(null);
                };
                bool pushed = await CottonShellNavigation.PushAsync(
                    page,
                    cancellationToken,
                    currentPage => currentPage is CaptureDestinationPickerPage);
                if (!pushed)
                {
                    completion.TrySetResult(null);
                }
            });

            await using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
            {
                return await completion.Task.ConfigureAwait(false);
            }
        }
    }
}
