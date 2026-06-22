// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
