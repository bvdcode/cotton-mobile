// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Auth;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public class MainPagePresentationService : IMainPagePresentationService
    {
        public MainPageProfile CreateProfile(Uri instanceUri, UserDto user)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(user);

            string displayName = CreateDisplayName(user);
            return new MainPageProfile(
                displayName,
                string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim(),
                CreateInstanceDisplayName(instanceUri),
                CreateAccountScopeKey(user, displayName));
        }

        public string ResolveStatusMessage(CottonSessionResult result, string unauthenticatedStatus)
        {
            ArgumentNullException.ThrowIfNull(result);

            return result.Status switch
            {
                CottonSessionResultStatus.Unauthenticated => unauthenticatedStatus,
                CottonSessionResultStatus.Authenticated => unauthenticatedStatus,
                CottonSessionResultStatus.AuthorizationDenied => AppResources.AuthorizationDenied,
                CottonSessionResultStatus.AuthorizationExpired => AppResources.AuthorizationExpired,
                CottonSessionResultStatus.AuthorizationNotFound => AppResources.AuthorizationNotFound,
                CottonSessionResultStatus.BrowserUnavailable => AppResources.BrowserUnavailable,
                CottonSessionResultStatus.TimedOut => AppResources.AuthorizationTimedOut,
                CottonSessionResultStatus.AuthorizationFailed => AppResources.AuthorizationFailed,
                CottonSessionResultStatus.SessionExpired => AppResources.SessionExpired,
                CottonSessionResultStatus.AuthorizationPending => AppResources.AuthorizationPending,
                _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, "Session result status is not supported."),
            };
        }

        public string CreateAuthorizationFailureStatus(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return AppResources.AuthorizationFailure;
        }

        private static string CreateDisplayName(UserDto user)
        {
            string fullName = string.Join(
                " ",
                new[] { user.FirstName, user.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))
                    .Select(part => part!.Trim()));
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }

            if (!string.IsNullOrWhiteSpace(user.Username))
            {
                return user.Username.Trim();
            }

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                return user.Email.Trim();
            }

            return AppResources.DefaultUserName;
        }

        private static string CreateInstanceDisplayName(Uri instanceUri)
        {
            string authority = instanceUri.IsDefaultPort
                ? instanceUri.Host
                : instanceUri.Authority;
            string path = instanceUri.AbsolutePath.TrimEnd('/');
            return string.IsNullOrWhiteSpace(path) || string.Equals(path, "/", StringComparison.Ordinal)
                ? authority
                : $"{authority}{path}";
        }

        private static string CreateAccountScopeKey(UserDto user, string displayName)
        {
            string identity = !string.IsNullOrWhiteSpace(user.Username)
                ? user.Username
                : !string.IsNullOrWhiteSpace(user.Email)
                    ? user.Email
                    : displayName;
            CottonAccountScopeKey.TryCreateFromUsername(identity, out string accountScopeKey);
            return accountScopeKey;
        }
    }
}
