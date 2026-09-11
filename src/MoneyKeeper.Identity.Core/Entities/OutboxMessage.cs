namespace MoneyKeeper.Identity.Core.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; set; }

        public string MessageType { get; set; } = string.Empty;

        public string RoutingKey { get; set; } = string.Empty;

        public string Payload {  get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public int AttemptCount { get; set; }

        public bool IsAbandoned { get; set; }

        public string? LastError { get; set; }

        public DateTime? LastAttemptAt { get; set; }
    }
}
