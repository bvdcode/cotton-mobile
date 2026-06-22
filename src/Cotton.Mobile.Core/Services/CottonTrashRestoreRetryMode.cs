// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonTrashRestoreRetryMode
    {
        None = 0,
        CreateMissingParents = 1,
        Overwrite = 2,
    }
}
