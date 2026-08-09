namespace MoneyKeeper.Identity.Application.Contracts.Auth
{
    public record AuthResult(string Email, string UserName, int Id, DateTime CreatedAt, string AccessToken, string RefreshToken);
}
