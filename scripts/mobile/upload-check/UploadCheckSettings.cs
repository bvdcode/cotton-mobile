namespace Cotton.Mobile.UploadChecks
{
    internal record UploadCheckSettings(
        Uri Server,
        string AccountFile,
        string Serial,
        string Package,
        string AndroidSourceDirectory,
        Guid CloudFolderId,
        string OutputDirectory);
}
