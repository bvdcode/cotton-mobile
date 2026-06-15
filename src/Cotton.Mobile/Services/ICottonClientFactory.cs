using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public interface ICottonClientFactory
    {
        ICottonCloudClient Create(Uri instanceUri);
    }
}
