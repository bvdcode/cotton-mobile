// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonContentRevisionIndexSnapshot
    {
        private readonly Dictionary<string, CottonContentRevisionSnapshot> _revisionsBySourceId;

        public CottonContentRevisionIndexSnapshot(
            string sourceVersion,
            IEnumerable<CottonContentRevisionSnapshot> revisions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceVersion);
            ArgumentNullException.ThrowIfNull(revisions);

            SourceVersion = sourceVersion.Trim();
            List<CottonContentRevisionSnapshot> orderedRevisions = [.. revisions
                .OrderBy(revision => revision.LocalSourceId, StringComparer.Ordinal)];
            Dictionary<string, CottonContentRevisionSnapshot> revisionsBySourceId =
                new(orderedRevisions.Count, StringComparer.Ordinal);
            foreach (CottonContentRevisionSnapshot revision in orderedRevisions)
            {
                if (!revisionsBySourceId.TryAdd(revision.LocalSourceId, revision))
                {
                    throw new ArgumentException("Content revision index contains duplicate source ids.", nameof(revisions));
                }
            }

            Revisions = orderedRevisions;
            _revisionsBySourceId = revisionsBySourceId;
        }

        public string SourceVersion { get; }

        public IReadOnlyList<CottonContentRevisionSnapshot> Revisions { get; }

        public bool HasSameContentAs(CottonContentRevisionIndexSnapshot? other)
        {
            if (other is null
                || !string.Equals(SourceVersion, other.SourceVersion, StringComparison.Ordinal)
                || Revisions.Count != other.Revisions.Count)
            {
                return false;
            }

            for (int index = 0; index < Revisions.Count; index++)
            {
                CottonContentRevisionSnapshot revision = Revisions[index];
                CottonContentRevisionSnapshot otherRevision = other.Revisions[index];
                if (!string.Equals(revision.LocalSourceId, otherRevision.LocalSourceId, StringComparison.Ordinal)
                    || revision.Generation != otherRevision.Generation
                    || !string.Equals(revision.ContentHash, otherRevision.ContentHash, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryGetContentHash(
            string localSourceId,
            long generation,
            out string? contentHash)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(localSourceId);
            if (generation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(generation), "Content generation cannot be negative.");
            }

            if (_revisionsBySourceId.TryGetValue(localSourceId, out CottonContentRevisionSnapshot? revision)
                && revision.Generation == generation)
            {
                contentHash = revision.ContentHash;
                return true;
            }

            contentHash = null;
            return false;
        }
    }
}
