using Microsoft.Maui.Networking;

namespace Cotton.Mobile.Services
{
    public class NetworkAccessService : INetworkAccessService
    {
        private readonly IConnectivity _connectivity;
        private NetworkAccess _networkAccess;

        public NetworkAccessService(IConnectivity connectivity)
        {
            ArgumentNullException.ThrowIfNull(connectivity);

            _connectivity = connectivity;
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

            InternetAccessRestored?.Invoke(this, EventArgs.Empty);
        }

        private NetworkAccess GetCurrentNetworkAccess()
        {
            _networkAccess = _connectivity.NetworkAccess;
            return _networkAccess;
        }
    }
}
