namespace ETL_simulator.ViewModels
{
    public enum LogLevel { Info, Warning, Error }

    public class LogEntry
    {
        public DateTime Time { get; init; } = DateTime.Now;
        public LogLevel Level { get; init; }
        public string Message { get; init; } = string.Empty;

        public string TimeText => Time.ToString("HH:mm:ss");

        public string LevelText => Level switch
        {
            LogLevel.Warning => "WARN",
            LogLevel.Error   => "ERR ",
            _                => "INFO"
        };

        public string Color => Level switch
        {
            LogLevel.Warning => "#FFC107",
            LogLevel.Error   => "#F44336",
            _                => "#B0BEC5"
        };
    }
}
