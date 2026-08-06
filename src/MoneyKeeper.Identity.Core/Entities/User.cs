namespace MoneyKeeper.Identity.Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public string UserName { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
