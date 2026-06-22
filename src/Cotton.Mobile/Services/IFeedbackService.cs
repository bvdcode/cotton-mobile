// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface IFeedbackService
    {
        Task<FeedbackDeliveryResult> OpenFeedbackAsync(
            FeedbackContext context,
            CancellationToken cancellationToken = default);

        Task CopyFeedbackAsync(FeedbackContext context, CancellationToken cancellationToken = default);
    }
}
