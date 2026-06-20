using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class AppLockGateService : IAppLockGateService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppLockGateService> _logger;

        public AppLockGateService(
            IServiceProvider serviceProvider,
            ILogger<AppLockGateService> logger)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);
            ArgumentNullException.ThrowIfNull(logger);

            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<CottonDeviceUnlockResult> ShowAndUnlockAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<CottonDeviceUnlockResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            AppLockGatePage? page = null;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                page = ActivatorUtilities.CreateInstance<AppLockGatePage>(_serviceProvider);
                if (page.BindingContext is not AppLockGateViewModel viewModel)
                {
                    throw new InvalidOperationException("App lock gate view model is unavailable.");
                }

                viewModel.SetCompletion(completion);
                await Shell.Current.Navigation.PushModalAsync(page, animated: false);
            });

            using CancellationTokenRegistration cancellationRegistration =
                cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            CottonDeviceUnlockResult result = await completion.Task.ConfigureAwait(false);
            if (!result.IsSucceeded)
            {
                return result;
            }

            await ClosePageAsync(page).ConfigureAwait(false);
            return result;
        }

        private async Task ClosePageAsync(AppLockGatePage? page)
        {
            if (page is null)
            {
                return;
            }

            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    IReadOnlyList<Page> modalStack = Shell.Current.Navigation.ModalStack;
                    if (modalStack.Contains(page))
                    {
                        await Shell.Current.Navigation.PopModalAsync(animated: false);
                    }
                });
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to close Cotton mobile app lock gate.");
            }
        }
    }
}
