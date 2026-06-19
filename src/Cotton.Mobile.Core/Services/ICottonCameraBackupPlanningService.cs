namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupPlanningService
    {
        Task<CottonCameraBackupPlanSnapshot> PlanAsync(
            Uri instanceUri,
            CottonCameraBackupSettings settings,
            CancellationToken cancellationToken = default);
    }
}
