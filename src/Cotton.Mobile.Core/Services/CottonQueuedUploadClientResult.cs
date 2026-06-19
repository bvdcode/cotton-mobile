namespace Cotton.Mobile.Services
{
    public sealed class CottonQueuedUploadClientResult
    {
        public CottonQueuedUploadClientResult(Guid? remoteFileId, string? remoteFileName)
        {
            RemoteFileId = remoteFileId == Guid.Empty ? null : remoteFileId;
            RemoteFileName = string.IsNullOrWhiteSpace(remoteFileName) ? null : remoteFileName.Trim();
        }

        public Guid? RemoteFileId { get; }

        public string? RemoteFileName { get; }
    }
}
