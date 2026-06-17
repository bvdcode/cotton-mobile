using Cotton.Mobile;
using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class DiagnosticsPageService : IDiagnosticsPageService
    {
        private readonly IServiceProvider _serviceProvider;

        public DiagnosticsPageService(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;
        }

        public Task OpenAsync(CottonDiagnosticsContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            cancellationToken.ThrowIfCancellationRequested();

            var viewModel = ActivatorUtilities.CreateInstance<DiagnosticsViewModel>(
                _serviceProvider,
                context);
            var page = ActivatorUtilities.CreateInstance<DiagnosticsPage>(
                _serviceProvider,
                viewModel);
            return MainThread.InvokeOnMainThreadAsync(
                () => Shell.Current.Navigation.PushAsync(page));
        }
    }
}
