namespace WindowsBox.Core.Crash;

public sealed class CrashReport
{
    public required string Id { get; init; }

    public required DateTimeOffset OccurredAtUtc { get; init; }

    public required string ExceptionType { get; init; }

    public required string Message { get; init; }

    public string? StackTrace { get; init; }

    public string? AppVersion { get; init; }

    public string? OsDescription { get; init; }
}

public sealed class CrashDumpWriteResult
{
    public required string DumpPath { get; init; }

    public required CrashReport Report { get; init; }
}

public sealed class CrashUploadResult
{
    public required bool Attempted { get; init; }

    public required bool Uploaded { get; init; }

    public required string Message { get; init; }

    public bool SkippedBecauseDisabled { get; init; }
}
