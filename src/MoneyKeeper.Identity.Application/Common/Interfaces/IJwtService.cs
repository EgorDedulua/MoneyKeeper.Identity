using MoneyKeeper.Identity.Core.Entities;

namespace MoneyKeeper.Identity.Application.Common.Interfaces
{
    public interface IJwtService
    {
        string Generate(User user);
    }
}
