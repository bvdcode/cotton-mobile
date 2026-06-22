// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFolderHandle
    {
        public CottonFolderHandle(Guid id, string name)
        {
            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? "Files" : name.Trim();
        }

        public Guid Id { get; }

        public string Name { get; }
    }
}
