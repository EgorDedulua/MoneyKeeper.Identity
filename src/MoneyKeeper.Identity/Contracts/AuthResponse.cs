namespace MoneyKeeper.Identity.Contracts
{
    public record AuthResponse(string Email, string UserName, int Id, DateTime CreatedAt, string AccessToken);
}
