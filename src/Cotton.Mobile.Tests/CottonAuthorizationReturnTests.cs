// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class CottonAuthorizationReturnTests
    {
        [Fact]
        public void CreateApprovalUri_AddsMobileReturnTarget()
        {
            Uri approvalUri = new("https://cotton.example/oauth/app-code/request-id");

            Uri result = CottonAuthorizationReturn.CreateApprovalUri(approvalUri);

            Assert.Equal(
                "https://cotton.example/oauth/app-code/request-id?returnTo=mobile",
                result.AbsoluteUri);
        }

        [Fact]
        public void CreateApprovalUri_PreservesExistingQuery()
        {
            Uri approvalUri = new("https://cotton.example/oauth/app-code/request-id?language=en");

            Uri result = CottonAuthorizationReturn.CreateApprovalUri(approvalUri);

            Assert.Equal(
                "https://cotton.example/oauth/app-code/request-id?language=en&returnTo=mobile",
                result.AbsoluteUri);
        }
    }
}
