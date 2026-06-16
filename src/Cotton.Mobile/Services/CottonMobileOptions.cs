namespace Cotton.Mobile.Services
{
    public class CottonMobileOptions
    {
        public CottonMobileOptions(
            string applicationName,
            Uri defaultInstanceUri,
            Uri privacyPolicyUri)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
            {
                throw new ArgumentException("Application name is required.", nameof(applicationName));
            }

            ArgumentNullException.ThrowIfNull(defaultInstanceUri);
            ArgumentNullException.ThrowIfNull(privacyPolicyUri);
            CottonInstanceUri.EnsureSupported(defaultInstanceUri, nameof(defaultInstanceUri));
            EnsureHttpsUri(privacyPolicyUri, nameof(privacyPolicyUri));

            ApplicationName = applicationName.Trim();
            DefaultInstanceUri = defaultInstanceUri;
            PrivacyPolicyUri = privacyPolicyUri;
        }

        public string ApplicationName { get; }

        public Uri DefaultInstanceUri { get; }

        public Uri PrivacyPolicyUri { get; }

        public string DefaultInstanceUrl => DefaultInstanceUri.AbsoluteUri;

        private static void EnsureHttpsUri(Uri uri, string parameterName)
        {
            if (!uri.IsAbsoluteUri
                || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(uri.Host))
            {
                throw new ArgumentException("URI must be an absolute HTTPS URI.", parameterName);
            }
        }
    }
}
