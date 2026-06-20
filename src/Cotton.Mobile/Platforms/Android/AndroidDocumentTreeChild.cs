#if ANDROID
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeChild
    {
        public AndroidDocumentTreeChild(AndroidUri uri, string documentId, string displayName, string mimeType)
        {
            Uri = uri;
            DocumentId = documentId;
            DisplayName = displayName;
            MimeType = mimeType;
        }

        public AndroidUri Uri { get; }

        public string DocumentId { get; }

        public string DisplayName { get; }

        public string MimeType { get; }

        public bool IsDirectory => string.Equals(
            MimeType,
            DocumentsContract.Document.MimeTypeDir,
            StringComparison.Ordinal);
    }
}
#endif
