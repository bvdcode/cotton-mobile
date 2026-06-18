using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class SecureStorageCottonPendingAppCodeSessionStore : ICottonPendingAppCodeSessionStore
    {
        private const string PendingSessionKey = "Cotton.Mobile.Auth.PendingAppCodeSession";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ISecureStorage _secureStorage;
        private readonly ILogger<SecureStorageCottonPendingAppCodeSessionStore> _logger;

        public SecureStorageCottonPendingAppCodeSessionStore(
            ISecureStorage secureStorage,
            ILogger<SecureStorageCottonPendingAppCodeSessionStore> logger)
        {
            ArgumentNullException.ThrowIfNull(secureStorage);
            ArgumentNullException.ThrowIfNull(logger);

            _secureStorage = secureStorage;
            _logger = logger;
        }

        public async Task<CottonPendingAppCodeSession?> GetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? value;
            try
            {
                value = await _secureStorage.GetAsync(PendingSessionKey).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to read Cotton mobile app-code authorization session; clearing it.");
                ClearBestEffort("pending authorization read failure");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            try
            {
                CottonPendingAppCodeSession? session =
                    JsonSerializer.Deserialize<CottonPendingAppCodeSession>(value, SerializerOptions);
                if (session is null || !IsValid(session))
                {
                    _logger.LogWarning("Stored Cotton mobile app-code authorization session is invalid; clearing it.");
                    ClearBestEffort("invalid pending authorization session");
                    return null;
                }

                session.ExpiresAt = NormalizeUtc(session.ExpiresAt);
                return session;
            }
            catch (JsonException exception)
            {
                _logger.LogWarning(exception, "Failed to parse stored Cotton mobile app-code authorization session; clearing it.");
                ClearBestEffort("pending authorization parse failure");
                return null;
            }
            catch (NotSupportedException exception)
            {
                _logger.LogWarning(exception, "Stored Cotton mobile app-code authorization session has an unsupported shape; clearing it.");
                ClearBestEffort("unsupported pending authorization session");
                return null;
            }
        }

        public async Task SaveAsync(
            CottonPendingAppCodeSession session,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(session);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsValid(session))
            {
                throw new ArgumentException("Pending app-code authorization session is invalid.", nameof(session));
            }

            var sessionToSave = new CottonPendingAppCodeSession
            {
                InstanceUri = session.InstanceUri,
                ApprovalId = session.ApprovalId,
                ApprovalUri = session.ApprovalUri,
                PollToken = session.PollToken,
                ExpiresAt = NormalizeUtc(session.ExpiresAt),
                PollInterval = session.PollInterval,
            };

            try
            {
                string value = JsonSerializer.Serialize(sessionToSave, SerializerOptions);
                await _secureStorage.SetAsync(PendingSessionKey, value).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile app-code authorization session; clearing it.");
                ClearBestEffort("pending authorization save failure");
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _secureStorage.Remove(PendingSessionKey);
            return Task.CompletedTask;
        }

        private void ClearBestEffort(string reason)
        {
            try
            {
                _secureStorage.Remove(PendingSessionKey);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to clear Cotton mobile app-code authorization session after {Reason}.",
                    reason);
            }
        }

        private static bool IsValid(CottonPendingAppCodeSession session)
        {
            return session.InstanceUri is not null
                && session.ApprovalUri is not null
                && CottonInstanceUri.IsSupported(session.InstanceUri)
                && session.ApprovalId != Guid.Empty
                && session.ApprovalUri.IsAbsoluteUri
                && string.Equals(session.ApprovalUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(session.PollToken)
                && session.ExpiresAt != default
                && session.PollInterval > TimeSpan.Zero;
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            return value.ToUniversalTime();
        }
    }
}
