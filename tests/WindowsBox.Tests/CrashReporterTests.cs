using System.Net;
using WindowsBox.Core.Crash;
using WindowsBox.Core.Settings;

namespace WindowsBox.Tests;

public class CrashReporterTests
{
    [Fact]
    public void WriteLocalDump_always_persists_without_network()
    {
        var dir = TempDir("crash");
        try
        {
            var store = new FileAppSettingsStore(Path.Combine(dir, "cfg"));
            var reporter = new CrashReporter(Path.Combine(dir, "dumps"), store);
            var written = reporter.WriteLocalDump(new InvalidOperationException("boom"), "0.1.0");
            Assert.True(File.Exists(written.DumpPath));
            Assert.Equal("boom", written.Report.Message);
            Assert.Contains(written.DumpPath, reporter.ListLocalDumps());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Upload_skipped_when_opt_in_off()
    {
        var dir = TempDir("crash-up");
        var calls = 0;
        try
        {
            var store = new FileAppSettingsStore(Path.Combine(dir, "cfg"));
            var reporter = new CrashReporter(
                Path.Combine(dir, "dumps"),
                store,
                httpClientFactory: () =>
                {
                    calls++;
                    return new HttpClient(new CountingHandler());
                },
                uploadEndpoint: "https://example.test/crash");

            var dump = reporter.WriteLocalDump(new Exception("x"));
            var result = await reporter.TryUploadAsync(dump.DumpPath);
            Assert.True(result.SkippedBecauseDisabled);
            Assert.False(result.Uploaded);
            Assert.Equal(0, calls);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task Upload_posts_when_opted_in()
    {
        var dir = TempDir("crash-ok");
        try
        {
            var store = new FileAppSettingsStore(Path.Combine(dir, "cfg"));
            store.Save(new AppSettings { CrashReportUploadEnabled = true });
            var handler = new CountingHandler();
            var reporter = new CrashReporter(
                Path.Combine(dir, "dumps"),
                store,
                httpClientFactory: () => new HttpClient(handler),
                uploadEndpoint: "https://example.test/crash");

            var dump = reporter.WriteLocalDump(new Exception("x"));
            var result = await reporter.TryUploadAsync(dump.DumpPath);
            Assert.True(result.Attempted);
            Assert.True(result.Uploaded);
            Assert.Equal(1, handler.Calls);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string TempDir(string prefix)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"wbox-{prefix}-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}
