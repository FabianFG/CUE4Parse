using System;

namespace CUE4Parse_Conversion.V2;

public readonly record struct ExportFile(string Extension, byte[] Data, string NameSuffix = "");

public sealed record ExportResult(bool Success, string ObjectName, string PackagePath, string PackageDirectory, ExportFile? File = null, Exception? Error = null)
{
    public static ExportResult Failure(string objectName, string packagePath, string packageDirectory, Exception ex) => new(false, objectName, packagePath, packageDirectory, null, ex);
}

public readonly record struct ExportProgress(int Completed, int Total, ExportResult? LastResult = null)
{
    public float Percentage => Total > 0 ? Completed / (float)Total : -1f;
    public string DisplayText => Total > 0 ? $"{Completed} / {Total}" : $"{Completed}";
}

