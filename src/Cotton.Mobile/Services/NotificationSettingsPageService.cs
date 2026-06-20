using Cotton.Mobile;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class NotificationSettingsPageService : INotificationSettingsPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public NotificationSettingsPageService(IServiceProvider serviceProvider)
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
                if (Shell.Current.Navigation.NavigationStack.LastOrDefault() is NotificationSettingsPage currentPage
                    && currentPage.BindingContext is NotificationSettingsViewModel currentViewModel)
                {
                    currentViewModel.LoadCommand.Execute(null);
                    return;
                }

                var page = ActivatorUtilities.CreateInstance<NotificationSettingsPage>(_serviceProvider);
                await Shell.Current.Navigation.PushAsync(page);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
