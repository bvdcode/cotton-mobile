// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public class CottonFileDescriptor(
        Guid id,
        CottonFileBrowserEntryType type,
        string name,
        CottonFileKind kind)
    {
        public Guid Id { get; } = id;

        public CottonFileBrowserEntryType Type { get; } = Enum.IsDefined(type)
            ? type
            : throw new ArgumentOutOfRangeException(nameof(type));

        public string Name { get; } = string.IsNullOrWhiteSpace(name)
            ? CoreResources.UnnamedItem
            : name.Trim();

        public CottonFileKind Kind { get; } = Enum.IsDefined(kind)
            ? kind
            : throw new ArgumentOutOfRangeException(nameof(kind));
    }
}
