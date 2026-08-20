using MoneyKeeper.Identity.Application.Events;

namespace MoneyKeeper.Identity.Application.Common.Interfaces.Messaging
{
    public interface IMessageBus
    {
        Task PublishUserRegisteredAsync(UserRegisteredEvent message, CancellationToken cancellationToken = default);

        Task PublishUserDeletedAsync(UserDeletedEvent message, CancellationToken cancellationToken = default);
    }
}
