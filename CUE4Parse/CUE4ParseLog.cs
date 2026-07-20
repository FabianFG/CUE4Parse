using Serilog;
using Serilog.Core;

namespace CUE4Parse;

/// <summary>
/// Provides the logger used by CUE4Parse.
/// </summary>
public static class CUE4ParseLog
{
    private const string SourceContext = "CUE4Parse";
    private static ILogger _logger = WithSourceContext(Serilog.Log.Logger);

    /// <summary>
    /// Gets the logger used internally by CUE4Parse.
    /// </summary>
    public static ILogger Log => Volatile.Read(ref _logger);

    /// <summary>
    /// Configures CUE4Parse to use <paramref name="logger"/> while identifying all
    /// emitted events with a <c>SourceContext</c> of <c>CUE4Parse</c>.
    /// </summary>
    public static void UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        Volatile.Write(ref _logger, WithSourceContext(logger));
    }

    private static ILogger WithSourceContext(ILogger logger) =>
        logger.ForContext(Constants.SourceContextPropertyName, SourceContext);
}
