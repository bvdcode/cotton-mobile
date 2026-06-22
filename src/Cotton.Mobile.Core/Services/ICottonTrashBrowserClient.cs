// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public interface ICottonTrashBrowserClient
    {
        Task<NodeDto> GetTrashRootAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default);

        Task<NodeContentDto> GetChildrenAsync(
            Uri instanceUri,
            Guid trashFolderId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
