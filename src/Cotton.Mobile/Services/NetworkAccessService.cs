using Microsoft.Extensions.Logging;
using Microsoft.Maui.Networking;

namespace Cotton.Mobile.Services
{
    public class NetworkAccessService : INetworkAccessService
    {
        private readonly IConnectivity _connectivity;
        private readonly ILogger<NetworkAccessService> _logger;
        private NetworkAccess _networkAccess;

        public NetworkAccessService(
            IConnectivity connectivity,
            ILogger<NetworkAccessService> logger)
        {
            ArgumentNullException.ThrowIfNull(connectivity);
            ArgumentNullException.ThrowIfNull(logger);

            _connectivity = connectivity;
            _logger = logger;
            _networkAccess = connectivity.NetworkAccess;
            _connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
        }

        public event EventHandler? InternetAccessRestored;

        public bool HasInternetAccess => GetCurrentNetworkAccess() == NetworkAccess.Internet;

        private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            NetworkAccess previousAccess = _networkAccess;
            _networkAccess = e.NetworkAccess;
            if (previousAccess == NetworkAccess.Internet || _networkAccess != NetworkAccess.Internet)
            {
                return;
            }

            NotifyInternetAccessRestored();
        }

        private void NotifyInternetAccessRestored()
        {
            EventHandler? handlers = InternetAccessRestored;
            if (handlers is null)
            {
                return;
            }

            foreach (EventHandler handler in handlers.GetInvocationList().Cast<EventHandler>())
            {
                try
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(exception, "Cotton mobile internet-restored subscriber failed.");
                }
            }
        }

        private NetworkAccess GetCurrentNetworkAccess()
        {
            _networkAccess = _connectivity.NetworkAccess;
            return _networkAccess;
        }
    }
}
