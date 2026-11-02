namespace WindowsBox.Core.Logging;

public interface IAppLogger
{
    LogLevel MinimumLevel { get; set; }

    void Log(LogLevel level, string message);

    void Trace(string message);

    void Debug(string message);

    void Info(string message);

    void Warning(string message);

    void Error(string message);
}

/// <summary>
/// File + in-memory leveled logger. Never phones home.
/// </summary>
public sealed class FileAppLogger : IAppLogger
{
    private readonly string _logPath;
    private readonly object _gate = new();
    private readonly List<string> _recent = [];

    public FileAppLogger(string logDirectory, LogLevel minimumLevel = LogLevel.Info)
    {
        Directory.CreateDirectory(logDirectory);
        _logPath = Path.Combine(logDirectory, "windowsbox.log");
        MinimumLevel = minimumLevel;
    }

    public LogLevel MinimumLevel { get; set; }

    public string LogPath => _logPath;

    public IReadOnlyList<string> RecentEntries
    {
        get
        {
            lock (_gate)
            {
                return _recent.ToList();
            }
        }
    }

    public void Trace(string message) => Log(LogLevel.Trace, message);

    public void Debug(string message) => Log(LogLevel.Debug, message);

    public void Info(string message) => Log(LogLevel.Info, message);

    public void Warning(string message) => Log(LogLevel.Warning, message);

    public void Error(string message) => Log(LogLevel.Error, message);

    public void Log(LogLevel level, string message)
    {
        if (level < MinimumLevel || MinimumLevel == LogLevel.None)
        {
            return;
        }

        var line = $"{DateTimeOffset.UtcNow:u} [{level}] {message}";
        lock (_gate)
        {
            _recent.Add(line);
            if (_recent.Count > 500)
            {
                _recent.RemoveAt(0);
            }

            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
    }
}
