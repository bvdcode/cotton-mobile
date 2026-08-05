// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Runtime.ExceptionServices;

namespace Cotton.Mobile.Services
{
    public class CottonRecoverableDocumentReplacement<TDocument>
        where TDocument : notnull
    {
        private readonly ICottonDocumentMutationStore<TDocument> _store;

        public CottonRecoverableDocumentReplacement(ICottonDocumentMutationStore<TDocument> store)
        {
            ArgumentNullException.ThrowIfNull(store);

            _store = store;
        }

        public TDocument Replace(
            TDocument replacement,
            TDocument current,
            string finalName,
            string backupName)
        {
            if (string.IsNullOrWhiteSpace(finalName))
            {
                throw new ArgumentException("Final document name is required.", nameof(finalName));
            }

            if (string.IsNullOrWhiteSpace(backupName))
            {
                throw new ArgumentException("Backup document name is required.", nameof(backupName));
            }

            TDocument backup = _store.Rename(current, backupName.Trim());
            TDocument promoted;
            try
            {
                promoted = _store.Rename(replacement, finalName.Trim());
            }
            catch (Exception promotionException)
            {
                RollBack(replacement, backup, finalName.Trim(), promotionException);
                throw;
            }

            _store.Delete(backup);
            return promoted;
        }

        private void RollBack(
            TDocument replacement,
            TDocument backup,
            string finalName,
            Exception promotionException)
        {
            try
            {
                _store.Rename(backup, finalName);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "Document promotion and rollback both failed; the backup was preserved.",
                    promotionException,
                    rollbackException);
            }

            try
            {
                _store.Delete(replacement);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "Document promotion failed and temporary-document cleanup also failed.",
                    promotionException,
                    cleanupException);
            }

            ExceptionDispatchInfo.Capture(promotionException).Throw();
        }
    }
}
