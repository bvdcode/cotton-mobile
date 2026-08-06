// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    /// <summary>
    /// Builds the preview URL that serves a user avatar.
    /// </summary>
    /// <remarks>
    /// The server encrypts the avatar hash with an authenticated cipher and serves the
    /// preview endpoint anonymously, so the token itself is the capability and the request
    /// carries no credentials.
    /// </remarks>
    public static class CottonAvatarUrl
    {
        private const string PreviewExtension = ".webp";

        /// <summary>
        /// Creates the avatar URL for an instance, or <see langword="null"/> when the
        /// account has no avatar.
        /// </summary>
        /// <param name="instanceUri">Instance the account belongs to.</param>
        /// <param name="avatarHashEncryptedHex">Encrypted avatar hash token reported by the server.</param>
        /// <returns>An absolute avatar URL, or <see langword="null"/> when no avatar is set.</returns>
        public static Uri? TryCreate(Uri instanceUri, string? avatarHashEncryptedHex)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            string token = avatarHashEncryptedHex?.Trim() ?? string.Empty;
            if (token.Length == 0)
            {
                return null;
            }

            return new Uri(
                instanceUri,
                $"{Routes.V1.Previews}/{Uri.EscapeDataString(token)}{PreviewExtension}");
        }
    }
}
