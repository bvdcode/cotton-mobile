// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonAccountScopeKey
    {
        private const string UserIdPrefix = "user-id:";

        public static string Create(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            return UserIdPrefix + userId.ToString("D");
        }
    }
}
