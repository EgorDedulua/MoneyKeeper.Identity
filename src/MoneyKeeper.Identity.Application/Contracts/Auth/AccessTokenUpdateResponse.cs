namespace MoneyKeeper.Identity.Application.Contracts.Auth
{
    public record AccessTokenUpdateResponse(string AccessToken, string RefreshToken);
}
