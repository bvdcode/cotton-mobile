using Cotton.Auth;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public class MainPagePresentationService : IMainPagePresentationService
    {
        public MainPageProfile CreateProfile(Uri instanceUri, UserDto user)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(user);

            return new MainPageProfile(
                CreateDisplayName(user),
                string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim(),
                CreateInstanceDisplayName(instanceUri));
        }

        public string ResolveStatusMessage(CottonSessionResult result, string unauthenticatedStatus)
        {
            ArgumentNullException.ThrowIfNull(result);

            return result.Status switch
            {
                CottonSessionResultStatus.AuthorizationDenied => "Authorization was denied.",
                CottonSessionResultStatus.AuthorizationExpired => "Authorization expired. Try again.",
                CottonSessionResultStatus.AuthorizationNotFound => "Authorization request was not found. Try again.",
                CottonSessionResultStatus.BrowserUnavailable => "Could not open the browser.",
                CottonSessionResultStatus.TimedOut => "Authorization timed out. Try again.",
                CottonSessionResultStatus.AuthorizationFailed => "Authorization failed. Try again.",
                CottonSessionResultStatus.SessionExpired => "Session expired. Sign in again.",
                CottonSessionResultStatus.AuthorizationPending => "Authorization is still pending. Return after approving in your browser.",
                _ => unauthenticatedStatus,
            };
        }

        public string CreateAuthorizationFailureStatus(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return "Authorization failed. Check the instance URL and try again.";
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

            return "Cotton user";
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
    }
}
