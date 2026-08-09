using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyKeeper.Identity.Core.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public int UserId { get; set; }

        public string? PreviousTokenHash { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public DateTime? RevokedAt { get; set; } = null;

        [NotMapped]
        public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

        public User User { get; set; } = null!;
    }
}
