namespace MoneyKeeper.Identity.Application.Outbox
{
    public class OutboxSettings
    {
        public int BatchSize { get; set; } = 50;

        public int PollIntervalSeconds { get; set; } = 2;

        public int RetainProcessedDays { get; set; } = 7;

        public int DeleteBatchSize { get; set; } = 500;

        public int CleanupIntervalHours { get; set; } = 6;

        public int StaleThresholdHours { get; set; } = 48;

        public int StaleBatchLimit { get; set; } = 100;

        public int MaxAttempts { get; set; } = 10;

    }
}
