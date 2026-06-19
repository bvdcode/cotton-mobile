namespace Cotton.Mobile.Services
{
    public static class CottonCloudShareLinkUrlBuilder
    {
        private const string TokenQueryParameterName = "token";
        private static readonly Uri TokenExtractionBaseUri = new("https://cotton.invalid");

        public static Uri CreateShareUri(Uri instanceUri, string token)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentException.ThrowIfNullOrWhiteSpace(token);

            string path = CreateSharePath(instanceUri, token);
            var builder = new UriBuilder(instanceUri)
            {
                Path = path,
                Query = string.Empty,
                Fragment = string.Empty,
            };
            return builder.Uri;
        }

        public static string BuildShareUrl(Uri instanceUri, string backendLink)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(backendLink);
            string token = ExtractToken(backendLink)
                ?? throw new ArgumentException("Share link token was not found.", nameof(backendLink));
            return CreateShareUri(instanceUri, token).AbsoluteUri;
        }

        public static string? ExtractToken(string backendLink)
        {
            if (string.IsNullOrWhiteSpace(backendLink))
            {
                return null;
            }

            string trimmed = backendLink.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? absoluteUri)
                && IsHttpUri(absoluteUri))
            {
                return ExtractToken(absoluteUri);
            }

            if (Uri.TryCreate(TokenExtractionBaseUri, trimmed, out Uri? relativeUri))
            {
                return ExtractToken(relativeUri);
            }

            return ExtractTokenFromPath(trimmed.Split('?', 2)[0]);
        }

        private static string CreateSharePath(Uri instanceUri, string token)
        {
            string basePath = instanceUri.AbsolutePath.TrimEnd('/');
            string encodedToken = Uri.EscapeDataString(token);
            return string.IsNullOrWhiteSpace(basePath) || basePath == "/"
                ? $"/s/{encodedToken}"
                : $"{basePath}/s/{encodedToken}";
        }

        private static string? ExtractToken(Uri uri)
        {
            string? queryToken = ExtractQueryParameter(uri.Query, TokenQueryParameterName);
            return string.IsNullOrWhiteSpace(queryToken)
                ? ExtractTokenFromPath(uri.AbsolutePath)
                : queryToken;
        }

        private static string? ExtractQueryParameter(string query, string name)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            string normalizedQuery = query.TrimStart('?');
            foreach (string part in normalizedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = part.Split('=', 2);
                string key = Uri.UnescapeDataString(pair[0]);
                if (!string.Equals(key, name, StringComparison.Ordinal))
                {
                    continue;
                }

                if (pair.Length < 2 || string.IsNullOrWhiteSpace(pair[1]))
                {
                    return null;
                }

                return Uri.UnescapeDataString(pair[1]);
            }

            return null;
        }

        private static string? ExtractTokenFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string[] parts = path
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.UnescapeDataString)
                .ToArray();
            return parts.Length == 0 || string.IsNullOrWhiteSpace(parts[^1])
                ? null
                : parts[^1];
        }

        private static bool IsHttpUri(Uri uri)
        {
            return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }
    }
}
