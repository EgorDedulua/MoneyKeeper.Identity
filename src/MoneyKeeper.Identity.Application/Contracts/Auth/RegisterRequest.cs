namespace MoneyKeeper.Identity.Application.Contracts.Auth
{
    public record RegisterRequest(string Email, string UserName, string Password);
}
