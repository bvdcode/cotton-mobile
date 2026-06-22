// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonUserStorageQuotaDto
    {
        public long UsedBytes { get; set; }

        public long? QuotaBytes { get; set; }

        public long? AvailableBytes { get; set; }
    }
}
