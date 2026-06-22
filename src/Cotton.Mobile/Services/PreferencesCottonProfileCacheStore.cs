// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public class PreferencesCottonProfileCacheStore : ICottonProfileCacheStore
    {
        private const string InstanceUrlKey = "Cotton.Mobile.Profile.InstanceUrl";
        private const string NameKey = "Cotton.Mobile.Profile.Name";
        private const string EmailKey = "Cotton.Mobile.Profile.Email";
        private const string InstanceDisplayKey = "Cotton.Mobile.Profile.InstanceDisplay";

        private readonly IPreferences _preferences;
        private readonly ILogger<PreferencesCottonProfileCacheStore> _logger;

        public PreferencesCottonProfileCacheStore(
            IPreferences preferences,
            ILogger<PreferencesCottonProfileCacheStore> logger)
        {
            ArgumentNullException.ThrowIfNull(preferences);
            ArgumentNullException.ThrowIfNull(logger);

            _preferences = preferences;
            _logger = logger;
        }

        public Task<MainPageProfile?> GetAsync(Uri instanceUri, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(instanceUri);
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            try
            {
                string savedInstanceUrl = _preferences.Get(InstanceUrlKey, string.Empty) ?? string.Empty;
                if (!string.Equals(savedInstanceUrl, instanceUri.AbsoluteUri, StringComparison.Ordinal))
                {
                    return Task.FromResult<MainPageProfile?>(null);
                }

                string name = _preferences.Get(NameKey, string.Empty) ?? string.Empty;
                string? email = _preferences.Get(EmailKey, string.Empty);
                string instanceDisplay = _preferences.Get(InstanceDisplayKey, string.Empty) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(instanceDisplay))
                {
                    ClearBestEffort("incomplete profile cache");
                    return Task.FromResult<MainPageProfile?>(null);
                }

                return Task.FromResult<MainPageProfile?>(
                    new MainPageProfile(name, email, instanceDisplay));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Failed to read Cotton mobile cached profile.");
                ClearBestEffort("profile cache read failure");
                return Task.FromResult<MainPageProfile?>(null);
            }
        }

        public Task SaveAsync(
            Uri instanceUri,
            MainPageProfile profile,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(profile);
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            try
            {
                _preferences.Set(InstanceUrlKey, instanceUri.AbsoluteUri);
                _preferences.Set(NameKey, profile.Name);
                if (string.IsNullOrWhiteSpace(profile.Email))
                {
                    _preferences.Remove(EmailKey);
                }
                else
                {
                    _preferences.Set(EmailKey, profile.Email);
                }

                _preferences.Set(InstanceDisplayKey, profile.Instance);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Failed to save Cotton mobile cached profile.");
                ClearBestEffort("profile cache save failure");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Exception> failures = [];
            RemoveKey(InstanceUrlKey, failures);
            RemoveKey(NameKey, failures);
            RemoveKey(EmailKey, failures);
            RemoveKey(InstanceDisplayKey, failures);
            if (failures.Count == 1)
            {
                throw new InvalidOperationException("Failed to clear one Cotton mobile cached profile value.", failures[0]);
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Failed to clear Cotton mobile cached profile.", failures);
            }

            return Task.CompletedTask;
        }

        private void ClearBestEffort(string reason)
        {
            List<Exception> failures = [];
            RemoveKey(InstanceUrlKey, failures);
            RemoveKey(NameKey, failures);
            RemoveKey(EmailKey, failures);
            RemoveKey(InstanceDisplayKey, failures);
            foreach (Exception exception in failures)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile cached profile after {Reason}.", reason);
            }
        }

        private void RemoveKey(string key, List<Exception> failures)
        {
            try
            {
                _preferences.Remove(key);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
    }
}
