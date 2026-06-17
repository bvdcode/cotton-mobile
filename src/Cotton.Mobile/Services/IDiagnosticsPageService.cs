namespace Cotton.Mobile.Services
{
    public interface IDiagnosticsPageService
    {
        Task OpenAsync(CottonDiagnosticsContext context, CancellationToken cancellationToken = default);
    }
}
