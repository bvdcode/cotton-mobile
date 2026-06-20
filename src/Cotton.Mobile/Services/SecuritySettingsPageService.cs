using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class SecuritySettingsPageService : ISecuritySettingsPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public SecuritySettingsPageService(IServiceProvider serviceProvider)
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
                if (Shell.Current.Navigation.NavigationStack.LastOrDefault() is SecuritySettingsPage currentPage
                    && currentPage.BindingContext is SecuritySettingsViewModel currentViewModel)
                {
                    currentViewModel.LoadCommand.Execute(null);
                    return;
                }

                var page = ActivatorUtilities.CreateInstance<SecuritySettingsPage>(_serviceProvider);
                await Shell.Current.Navigation.PushAsync(page);
            });
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
