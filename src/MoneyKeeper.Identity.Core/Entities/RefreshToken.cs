using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyKeeper.Identity.Core.Entities
{
    public class RefreshToken
    {
        public string TokenHash { get; set; } = string.Empty;

        public int UserId { get; set; }

        public string? PreviousTokenHash { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [NotMapped]
        public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;

        [NotMapped]
        public bool IsReplaced => PreviousTokenHash is not null;

        public User User { get; set; } = null!;
    }
}
