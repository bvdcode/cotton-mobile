// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Provider;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidDocumentMutationStore : ICottonDocumentMutationStore<AndroidUri>
    {
        private readonly ContentResolver _resolver;

        public AndroidDocumentMutationStore(ContentResolver resolver)
        {
            ArgumentNullException.ThrowIfNull(resolver);

            _resolver = resolver;
        }

        public AndroidUri Rename(AndroidUri document, string displayName)
        {
            return DocumentsContract.RenameDocument(_resolver, document, displayName)
                ?? throw new IOException($"Could not rename document to {displayName}.");
        }

        public void Delete(AndroidUri document)
        {
            if (!DocumentsContract.DeleteDocument(_resolver, document))
            {
                throw new IOException("Could not delete document.");
            }
        }
    }
}
#endif
