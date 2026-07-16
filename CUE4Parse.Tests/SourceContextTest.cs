using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CUE4Parse.Tests;

public class SourceContextTest
{
    [Fact]
    public void NamespaceOverrideFiltersContextualLogs()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("CUE4Parse", LogEventLevel.Error)
            .WriteTo.Sink(sink)
            .CreateLogger();

        var contextualLogger = logger.ForContext<SourceContextTest>();
        contextualLogger.Warning("Filtered warning");
        contextualLogger.Error("Retained error");

        var logEvent = Assert.Single(sink.Events);
        Assert.Equal(LogEventLevel.Error, logEvent.Level);
        Assert.Equal(typeof(SourceContextTest).FullName, logEvent.Properties["SourceContext"].ToString().Trim('"'));
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
