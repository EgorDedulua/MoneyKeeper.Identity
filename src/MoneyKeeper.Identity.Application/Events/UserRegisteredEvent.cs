namespace MoneyKeeper.Identity.Application.Events
{
    public class UserRegisteredEvent
    {
        public int UserId { get; set; }

        public string Email { get; set; } = string.Empty;
    }
}
