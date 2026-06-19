namespace Cotton.Mobile.Services
{
    public interface ICottonShareContentStagingStore
    {
        Task<CottonShareStagedContentSnapshot> StageAsync(
            Guid intakeId,
            Guid itemId,
            string fileName,
            Stream content,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CottonShareStagedContentSnapshot>> ListAsync(
            CancellationToken cancellationToken = default);

        Task DeleteIntakeAsync(Guid intakeId, CancellationToken cancellationToken = default);

        Task CleanupAsync(
            IReadOnlyCollection<CottonShareIntakeSnapshot> inboxSnapshots,
            CancellationToken cancellationToken = default);
    }
}
