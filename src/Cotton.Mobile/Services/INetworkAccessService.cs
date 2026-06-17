namespace Cotton.Mobile.Services
{
    public interface INetworkAccessService
    {
        event EventHandler? InternetAccessRestored;

        bool HasInternetAccess { get; }
    }
}
