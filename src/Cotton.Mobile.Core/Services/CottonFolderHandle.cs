// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public class CottonFolderHandle(Guid id, string name)
    {
        public Guid Id { get; } = id;

        public string Name { get; } = string.IsNullOrWhiteSpace(name) ? CoreResources.FilesName : name.Trim();
    }
}
