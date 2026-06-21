#if ANDROID
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;
using Cotton.Mobile.Services;
using Java.IO;

namespace Cotton.Mobile
{
    public class AndroidPdfPreviewRenderer : IPdfPreviewRenderer
    {
        private const int MaxPageWidthPixels = 1440;
        private const int MaxPageHeightPixels = 2200;

        public Task<PdfPreviewDocumentSnapshot> RenderAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            return Task.Run(
                () => Render(filePath, cancellationToken),
                cancellationToken);
        }

        private static PdfPreviewDocumentSnapshot Render(
            string filePath,
            CancellationToken cancellationToken)
        {
            if (!System.IO.File.Exists(filePath))
            {
                throw new System.IO.FileNotFoundException("PDF preview file was not found.", filePath);
            }

            using ParcelFileDescriptor? descriptor = ParcelFileDescriptor.Open(
                new Java.IO.File(filePath),
                ParcelFileMode.ReadOnly);
            if (descriptor is null)
            {
                throw new InvalidOperationException("Could not open PDF preview file.");
            }

            using var renderer = new PdfRenderer(descriptor);
            var pages = new List<PdfPreviewPageSnapshot>(renderer.PageCount);
            for (int index = 0; index < renderer.PageCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pages.Add(RenderPage(renderer, index));
            }

            return new PdfPreviewDocumentSnapshot(renderer.PageCount, pages);
        }

        private static PdfPreviewPageSnapshot RenderPage(PdfRenderer renderer, int pageIndex)
        {
            using PdfRenderer.Page page = renderer.OpenPage(pageIndex);
            (int width, int height) = CreateRenderSize(page.Width, page.Height);
            using Bitmap bitmap = Bitmap.CreateBitmap(
                width,
                height,
                Bitmap.Config.Argb8888!);
            using var canvas = new Canvas(bitmap);
            canvas.DrawColor(Android.Graphics.Color.White);
            page.Render(bitmap, null, null, PdfRenderMode.ForDisplay);

            byte[] pageBytes = CreatePngBytes(bitmap);
            ImageSource imageSource = ImageSource.FromStream(() => new MemoryStream(pageBytes));
            return new PdfPreviewPageSnapshot(
                pageIndex + 1,
                width,
                height,
                imageSource);
        }

        private static (int Width, int Height) CreateRenderSize(int pageWidth, int pageHeight)
        {
            int safeWidth = Math.Max(1, pageWidth);
            int safeHeight = Math.Max(1, pageHeight);
            double scale = Math.Min(
                MaxPageWidthPixels / (double)safeWidth,
                MaxPageHeightPixels / (double)safeHeight);
            scale = Math.Min(scale, 3d);
            scale = Math.Max(scale, 1d);
            return (
                Math.Max(1, (int)Math.Round(safeWidth * scale)),
                Math.Max(1, (int)Math.Round(safeHeight * scale)));
        }

        private static byte[] CreatePngBytes(Bitmap bitmap)
        {
            using var output = new MemoryStream();
            Bitmap.CompressFormat pngFormat = Bitmap.CompressFormat.Png
                ?? throw new InvalidOperationException("PNG compression is not available.");
            bool compressed = bitmap.Compress(pngFormat, 100, output);
            if (!compressed)
            {
                throw new InvalidOperationException("Could not render PDF page image.");
            }

            return output.ToArray();
        }
    }
}
#endif
