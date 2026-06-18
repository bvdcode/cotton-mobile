using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Cotton.Sdk.Chunks;
using Cotton.Sdk.Files;
using Cotton.Sdk.Nodes;
using Cotton.Sdk.Realtime;
using Cotton.Sdk.Settings;
using Cotton.Sdk.Sync;

namespace Cotton.Mobile.Services
{
    public class OwnedCottonCloudClient : ICottonCloudClient
    {
        private readonly ICottonCloudClient _inner;
        private readonly HttpClient _httpClient;
        private bool _isDisposed;

        public OwnedCottonCloudClient(ICottonCloudClient inner, HttpClient httpClient)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(httpClient);

            _inner = inner;
            _httpClient = httpClient;
        }

        public ICottonAuthClient Auth => _inner.Auth;

        public ICottonSettingsClient Settings => _inner.Settings;

        public ICottonChunkClient Chunks => _inner.Chunks;

        public ICottonFileClient Files => _inner.Files;

        public ICottonNodeClient Nodes => _inner.Nodes;

        public ICottonSyncClient Sync => _inner.Sync;

        public ICottonRealtimeClient Realtime => _inner.Realtime;

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            try
            {
                await _inner.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                _httpClient.Dispose();
            }
        }
    }
}
