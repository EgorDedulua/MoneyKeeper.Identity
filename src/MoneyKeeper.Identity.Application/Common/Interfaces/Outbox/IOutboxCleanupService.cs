namespace MoneyKeeper.Identity.Application.Common.Interfaces.Outbox
{
    public interface IOutboxCleanupService
    {
        Task<int> DeleteProcessedBatchAsync(CancellationToken cancellationToken = default);

        Task ReportStaleMessagesAsync(CancellationToken cancellationToken = default);
    }
}
