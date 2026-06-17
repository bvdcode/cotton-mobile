using Microsoft.Maui.Networking;

namespace Cotton.Mobile.Services
{
    public class NetworkAccessService : INetworkAccessService
    {
        private readonly IConnectivity _connectivity;

        public NetworkAccessService(IConnectivity connectivity)
        {
            ArgumentNullException.ThrowIfNull(connectivity);

            _connectivity = connectivity;
        }

        public bool HasInternetAccess => _connectivity.NetworkAccess == NetworkAccess.Internet;
    }
}
