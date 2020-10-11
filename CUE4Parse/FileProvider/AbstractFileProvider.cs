using Serilog;

namespace CUE4Parse.FileProvider
{
    public abstract class AbstractFileProvider
    {
        protected static ILogger log = Log.ForContext<IFileProvider>();
    }
}