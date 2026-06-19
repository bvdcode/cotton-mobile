using Cotton.Mobile;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class CaptureInboxPageService : ICaptureInboxPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public CaptureInboxPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var viewModel = ActivatorUtilities.CreateInstance<CaptureInboxViewModel>(_serviceProvider);
                var page = ActivatorUtilities.CreateInstance<CaptureInboxPage>(_serviceProvider, viewModel);
                await Shell.Current.Navigation.PushAsync(page);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
