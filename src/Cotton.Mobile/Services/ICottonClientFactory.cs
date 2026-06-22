// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;

namespace Cotton.Mobile.Services
{
    public interface ICottonClientFactory
    {
        ICottonCloudClient Create(Uri instanceUri);
    }
}
