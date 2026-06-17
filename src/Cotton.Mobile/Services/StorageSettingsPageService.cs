using Cotton.Mobile;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class StorageSettingsPageService : IStorageSettingsPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public StorageSettingsPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public Task OpenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var page = ActivatorUtilities.CreateInstance<StoragePage>(_serviceProvider);
            return MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.Navigation.PushAsync(page));
        }
    }
}
