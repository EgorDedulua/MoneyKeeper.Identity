namespace MoneyKeeper.Identity.Application.Common.Interfaces.Outbox
{
    public interface IOutboxPublisherService
    {
        Task<int> PublishPendingBatchAsync(CancellationToken cancellationToken);
    }
}
