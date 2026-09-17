// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonBackgroundWindowExpiredException(Exception innerException)
        : TimeoutException("The background execution window ended; unfinished work remains queued.", innerException)
    {
    }
}
