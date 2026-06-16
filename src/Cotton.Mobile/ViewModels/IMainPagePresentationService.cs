using Cotton.Auth;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.ViewModels
{
    public interface IMainPagePresentationService
    {
        MainPageProfile CreateProfile(Uri instanceUri, UserDto user);

        string ResolveStatusMessage(CottonSessionResult result, string unauthenticatedStatus);

        string CreateAuthorizationFailureStatus(Exception exception);
    }
}
