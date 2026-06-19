namespace Cotton.Mobile.Services
{
    public enum CottonQueuedUploadExecutionStatus
    {
        NoQueuedUpload = 0,
        MissingDestination = 1,
        MissingStagedFile = 2,
        Completed = 3,
        Failed = 4,
    }
}
