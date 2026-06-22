// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileSizeFormatter
    {
        public static string Format(long bytes)
        {
            const long Kilobyte = 1024;
            const long Megabyte = Kilobyte * 1024;
            const long Gigabyte = Megabyte * 1024;

            return bytes switch
            {
                < Kilobyte => $"{bytes} B",
                < Megabyte => $"{bytes / (double)Kilobyte:0.#} KB",
                < Gigabyte => $"{bytes / (double)Megabyte:0.#} MB",
                _ => $"{bytes / (double)Gigabyte:0.#} GB",
            };
        }
    }
}
