namespace WindowsBox.Core.Startup;

public interface IStartupEnumerator
{
    IReadOnlyList<StartupEntry> Enumerate(StartupEnumerateOptions? options = null);
}

public sealed class StartupEnumerateOptions
{
    public bool HideMicrosoft { get; init; }

    public bool IncludeDrivers { get; init; } = true;

    public bool IncludeShellExtensions { get; init; } = true;

    public bool IncludeServices { get; init; } = true;

    public bool IncludeScheduledTasks { get; init; } = true;
}
