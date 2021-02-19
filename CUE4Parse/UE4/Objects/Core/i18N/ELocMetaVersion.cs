namespace CUE4Parse.UE4.Objects.Core.i18N
{
    public enum ELocMetaVersion : byte
    {
        /** Initial format. */
        Initial = 0,
        /** Added complete list of cultures compiled for the localization target. */
        AddedCompiledCultures,

        LatestPlusOne,
        Latest = LatestPlusOne - 1
    }
}