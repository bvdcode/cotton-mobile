// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public interface ICottonDocumentMutationStore<TDocument>
        where TDocument : notnull
    {
        TDocument Rename(TDocument document, string displayName);

        void Delete(TDocument document);
    }
}
