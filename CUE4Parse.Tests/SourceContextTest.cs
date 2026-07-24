using Serilog.Core;
using Serilog.Events;
using static CUE4Parse.CUE4ParseLog;

namespace CUE4Parse.Tests;

public class SourceContextTest
{
    [Fact]
    public void NamespaceOverrideFiltersCUE4ParseLogs()
    {
        var sink = new CollectingSink();
        using var logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("CUE4Parse", LogEventLevel.Error)
            .WriteTo.Sink(sink)
            .CreateLogger();

        var previousLogger = Log;
        try
        {
            CUE4ParseLog.UseLogger(logger);
            Log.Warning("Filtered warning");
            Log.Error("Retained error");
        }
        finally
        {
            CUE4ParseLog.UseLogger(previousLogger);
        }

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Error, logEvent.Level);
        Assert.Equal("CUE4Parse", logEvent.Properties["SourceContext"].ToString().Trim('"'));
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
