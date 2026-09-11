namespace MoneyKeeper.Identity.Application.Common.Interfaces
{
    public interface IMessageBus
    {
        Task PublishAsync(Guid messageId, string messageType, string payload, CancellationToken cancellationToken = default);
    }
}
