using WindowsBox.Core.Logging;

namespace WindowsBox.Tests;

public class LoggingTests
{
    [Fact]
    public void Logger_respects_minimum_level()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-log-" + Guid.NewGuid().ToString("N"));
        try
        {
            var logger = new FileAppLogger(dir, LogLevel.Warning);
            logger.Info("hidden");
            logger.Warning("shown");
            logger.Error("also");
            Assert.Equal(2, logger.RecentEntries.Count);
            Assert.Contains(logger.RecentEntries, e => e.Contains("[Warning]", StringComparison.Ordinal));
            Assert.DoesNotContain(logger.RecentEntries, e => e.Contains("[Info]", StringComparison.Ordinal));
            Assert.True(File.Exists(logger.LogPath));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void None_suppresses_all()
    {
        var dir = Path.Combine(Path.GetTempPath(), "wbox-log-" + Guid.NewGuid().ToString("N"));
        try
        {
            var logger = new FileAppLogger(dir, LogLevel.None);
            logger.Error("nope");
            Assert.Empty(logger.RecentEntries);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
