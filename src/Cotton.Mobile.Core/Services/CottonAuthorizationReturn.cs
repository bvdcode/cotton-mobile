// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonAuthorizationReturn
    {
        public const string Scheme = "cotton";
        public const string Host = "authorization-complete";

        private const string ReturnTarget = "mobile";

        public static Uri CreateApprovalUri(Uri approvalUri)
        {
            ArgumentNullException.ThrowIfNull(approvalUri);

            UriBuilder builder = new(approvalUri);
            string query = builder.Query.TrimStart('?');
            string separator = query.Length == 0 ? string.Empty : "&";
            builder.Query = $"{query}{separator}returnTo={ReturnTarget}";
            return builder.Uri;
        }
    }
}
