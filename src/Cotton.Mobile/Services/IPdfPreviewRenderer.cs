namespace Cotton.Mobile.Services
{
    public interface IPdfPreviewRenderer
    {
        Task<PdfPreviewDocumentSnapshot> RenderAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }
}
