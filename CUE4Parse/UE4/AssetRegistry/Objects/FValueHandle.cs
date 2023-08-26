namespace CUE4Parse.UE4.AssetRegistry.Objects
{
    public static class FValueHandle
    {
        public static string? GetString(FStore store, FValueId id)
        {
            return id.Type switch
            {
                EValueType.AnsiString => store.GetAnsiString(id.Index),
                EValueType.WideString => store.GetWideString(id.Index),
                EValueType.NumberlessName => store.NameMap[store.NumberlessNames[id.Index]].Name,
                EValueType.Name => store.Names[id.Index].Text,
                EValueType.NumberlessExportPath => store.NumberlessExportPaths[id.Index].ToString(),
                EValueType.ExportPath => store.ExportPaths[id.Index].ToString(),
                EValueType.LocalizedText => store.Texts[id.Index],
                _ => string.Empty
            };
        }
    }
}
