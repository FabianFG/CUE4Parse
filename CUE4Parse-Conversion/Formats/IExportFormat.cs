namespace CUE4Parse_Conversion.Formats;

public interface IExportFormat
{
    string DisplayName { get; }
    string Extension { get; }
    IReadOnlyList<string> Extensions => [Extension];
}
