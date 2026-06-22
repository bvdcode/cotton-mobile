// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Auth;

namespace Cotton.Mobile.Services
{
    public class CottonSessionResult
    {
        public CottonSessionResultStatus Status { get; set; }

        public Uri? InstanceUri { get; set; }

        public UserDto? User { get; set; }

        public string? Message { get; set; }

        public bool IsAuthenticated => Status == CottonSessionResultStatus.Authenticated;

        public static CottonSessionResult Unauthenticated(Uri? instanceUri = null)
        {
            return new CottonSessionResult
            {
                Status = CottonSessionResultStatus.Unauthenticated,
                InstanceUri = instanceUri,
            };
        }

        public static CottonSessionResult Authenticated(Uri instanceUri, UserDto user)
        {
            return new CottonSessionResult
            {
                Status = CottonSessionResultStatus.Authenticated,
                InstanceUri = instanceUri,
                User = user,
            };
        }

        public static CottonSessionResult FromStatus(CottonSessionResultStatus status, Uri instanceUri, string? message = null)
        {
            return new CottonSessionResult
            {
                Status = status,
                InstanceUri = instanceUri,
                Message = message,
            };
        }
    }
}
