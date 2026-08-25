namespace MoneyKeeper.Identity.Infrastructure.Messaging
{
    public class RabbitMqSettings
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";

        public string ExchangeName { get; set; } = "identity.events";
        public string UserRegisteredRoutingKey { get; set; } = "user.registered";
        public string UserDeletedRoutingKey { get; set; } = "user.deleted";
        public string UserRegisteredQueue { get; set; } = "user.registered.queue";
        public string UserDeletedQueue { get; set; } = "user.deleted.queue";

        public string DlExchange { get; set; } = "dead.letter.exchange";
        public string DlQueue { get; set; } = "dead.letter.queue";
        public string DlqRoutingKey { get; set; } = "undelivered";

        public string AppId { get; set; } = "MoneyKeeper.Identity";

        public int PublishTimeoutSeconds { get; set; } = 30;
        public int DlqPublishTimeoutSeconds { get; set; } = 20;
        public int RetryCount { get; set; } = 4;
        public int RetryDelayMilliseconds { get; set; } = 1000;

        public bool AutomaticRecoveryEnabled { get; set; } = true;
        public int NetworkRecoveryIntervalSeconds { get; set; } = 5;
        public int RequestedHeartbeatSeconds { get; set; } = 30;

        public int InitialConnectionRetryCount { get; set; } = 5;
        public int InitialConnectionRetryDelayMilliseconds { get; set; } = 2000;
        public int ContinuationTimeoutSeconds { get; set; } = 30;

        public double CircuitBreakerFailureRatio { get; set; } = 0.5;
        public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;
        public int CircuitBreakerMinimumThroughput { get; set; } = 5;
        public int CircuitBreakerBreakDurationSeconds {  get; set; } = 15;
    }
}
