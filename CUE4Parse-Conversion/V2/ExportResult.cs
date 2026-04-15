using System;

namespace CUE4Parse_Conversion.V2;

public readonly record struct ExportFile(string Extension, byte[] Data, string? NameSuffix = null);

public sealed record ExportResult(bool Success, string ObjectPath, string? DiskFilePath = null, Exception? Error = null)
{
    public static ExportResult Failure(string objectPath, Exception ex) => new(false, objectPath, null, ex);
}

public readonly record struct ExportProgress(int Completed, int Total, ExportResult? LastResult = null)
{
    public float Percentage => Total > 0 ? Completed / (float)Total : -1f;
    public string DisplayText => Total > 0 ? $"{Completed} / {Total}  ({Percentage * 100:F0}%)" : $"{Completed}";
}

