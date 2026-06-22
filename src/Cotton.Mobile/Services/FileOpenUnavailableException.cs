// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class FileOpenUnavailableException : InvalidOperationException
    {
        public FileOpenUnavailableException(string message)
            : base(message)
        {
        }
    }
}
