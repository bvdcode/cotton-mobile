// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.Logging;
namespace Cotton.Mobile.Services
{
    public class CloudFolderPickerService : ICloudFolderPickerService
    {
        private readonly ICottonFileBrowserService _fileBrowserService;
        private readonly ILoggerFactory _loggerFactory;

        public CloudFolderPickerService(
            ICottonFileBrowserService fileBrowserService,
            ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(fileBrowserService);
            ArgumentNullException.ThrowIfNull(loggerFactory);

            _fileBrowserService = fileBrowserService;
            _loggerFactory = loggerFactory;
        }

        public async Task<CottonUploadDestinationSnapshot?> PickAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            cancellationToken.ThrowIfCancellationRequested();

            TaskCompletionSource<CottonUploadDestinationSnapshot?> completion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
            CloudFolderPickerViewModel viewModel = new(
                instanceUri,
                _fileBrowserService,
                destination => completion.TrySetResult(destination),
                _loggerFactory.CreateLogger<CloudFolderPickerViewModel>());
            CloudFolderPickerPage page = new(viewModel);
            INavigation navigation = await ModalPageNavigation.ShowAsync(page);
            using CancellationTokenRegistration registration = cancellationToken.Register(
                () => completion.TrySetCanceled(cancellationToken));
            try
            {
                return await completion.Task.ConfigureAwait(false);
            }
            finally
            {
                await ModalPageNavigation.DismissAsync(navigation, page);
            }
        }
    }
}
