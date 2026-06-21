namespace Cotton.Mobile.Services
{
    public class PdfPreviewDocumentSnapshot
    {
        public PdfPreviewDocumentSnapshot(
            int totalPageCount,
            IReadOnlyList<PdfPreviewPageSnapshot> pages)
        {
            ArgumentNullException.ThrowIfNull(pages);
            if (totalPageCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalPageCount));
            }

            TotalPageCount = totalPageCount;
            Pages = pages;
        }

        public int TotalPageCount { get; }

        public IReadOnlyList<PdfPreviewPageSnapshot> Pages { get; }

        public bool HasPages => Pages.Count > 0;

        public string StatusText => TotalPageCount == 1
            ? "1 page"
            : $"{TotalPageCount} pages";
    }
}
