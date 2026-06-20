using System.Globalization;

namespace Cotton.Mobile.Services
{
    public static class CottonAccountSessionAuthTypeText
    {
        private const int Unknown = 0;
        private const int Credentials = 1;
        private const int Google = 2;
        private const int Clover = 3;
        private const int Microsoft = 4;
        private const int Apple = 5;
        private const int GitHub = 7;
        private const int GitLab = 8;
        private const int AzureAd = 41;
        private const int Keycloak = 47;
        private const int MagicLink = 62;
        private const int OneTimePassword = 63;
        private const int TimeBasedOneTimePassword = 64;
        private const int SmsOtp = 65;
        private const int EmailOtp = 66;
        private const int Passkey = 68;
        private const int WebAuthn = 69;
        private const int Fido2 = 70;
        private const int HardwareSecurityKey = 72;
        private const int BasicAuth = 77;
        private const int BearerToken = 78;
        private const int JsonWebToken = 79;
        private const int Anonymous = 81;
        private const int Guest = 82;

        public static string Format(int authType)
        {
            return authType switch
            {
                Credentials => "Password",
                Google => "Google",
                Clover => "Clover",
                Microsoft => "Microsoft",
                Apple => "Apple",
                GitHub => "GitHub",
                GitLab => "GitLab",
                AzureAd => "Azure AD",
                Keycloak => "Keycloak",
                MagicLink => "Magic link",
                OneTimePassword => "One-time password",
                TimeBasedOneTimePassword => "Authenticator code",
                SmsOtp => "SMS code",
                EmailOtp => "Email code",
                Passkey => "Passkey",
                WebAuthn => "WebAuthn",
                Fido2 => "FIDO2",
                HardwareSecurityKey => "Security key",
                BasicAuth => "Basic auth",
                BearerToken => "Bearer token",
                JsonWebToken => "JWT",
                Anonymous => "Anonymous",
                Guest => "Guest",
                Unknown => "Unknown sign-in",
                _ => "Auth type " + authType.ToString(CultureInfo.InvariantCulture),
            };
        }
    }
}
