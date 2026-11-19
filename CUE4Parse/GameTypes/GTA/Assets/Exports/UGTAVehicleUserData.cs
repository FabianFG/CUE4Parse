using CUE4Parse.GameTypes.GTA.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse.GameTypes.GTA.Assets.Exports;

public class UGTAVehicleUserData : UAssetUserData
{
    public FPackageIndex? WheelMesh { get; private set; }
    public float ReflectionPower { get; private set; }
    public float MetallicPaint1 { get; private set; }
	public float MetallicPaint2 { get; private set; }
	public bool bHasVertexAO { get; private set; }
	public float VertexAOStrength { get; private set; }
	public FPackageIndex?[]? remapMaterials { get; private set; }
	public FPackageIndex? LightOnMIC { get; private set; }
	public FPackageIndex? LightOnTailLightsMIC { get; private set; }
	public FPackageIndex? LightOffMIC { get; private set; }
	public FPackageIndex? TaxiLightOnMIC { get; private set; }
	public FVector2D LicensePlateScaling { get; private set; }
	public int LightIndex_LF { get; private set; }
	public int LightIndex_RF { get; private set; }
	public int LightIndex_LR { get; private set; }
	public int LightIndex_RR { get; private set; }
	public int TaxiLightIndex { get; private set; }
	public int LeftSirenIndex { get; private set; }
	public int RightSirenIndex { get; private set; }
	public int OtherSirenIndex { get; private set; }
	public FWindowSectionInfoContainer[]? WindowsByLOD  { get; private set; }
	public FDamageInfoContainer[]? SectionDamageData  { get; private set; }
	public FPackageIndex? WindowMIC { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        WheelMesh = GetOrDefault<FPackageIndex?>(nameof(WheelMesh));
        ReflectionPower = GetOrDefault<float>(nameof(ReflectionPower));
        MetallicPaint1 = GetOrDefault<float>(nameof(MetallicPaint1));
        MetallicPaint2 = GetOrDefault<float>(nameof(MetallicPaint2));
        bHasVertexAO = GetOrDefault<bool>(nameof(bHasVertexAO));
        VertexAOStrength = GetOrDefault<float>(nameof(VertexAOStrength));
        remapMaterials = GetOrDefault<FPackageIndex?[]?>(nameof(remapMaterials));
        LightOnMIC = GetOrDefault<FPackageIndex?>(nameof(LightOnMIC));
        LightOnTailLightsMIC = GetOrDefault<FPackageIndex?>(nameof(LightOnTailLightsMIC));
        LightOffMIC = GetOrDefault<FPackageIndex?>(nameof(LightOffMIC));
        TaxiLightOnMIC = GetOrDefault<FPackageIndex?>(nameof(TaxiLightOnMIC));
        LicensePlateScaling = GetOrDefault<FVector2D>(nameof(LicensePlateScaling));
        LightIndex_LF = GetOrDefault<int>(nameof(LightIndex_LF));
        LightIndex_RF = GetOrDefault<int>(nameof(LightIndex_RF));
        LightIndex_LR = GetOrDefault<int>(nameof(LightIndex_LR));
        LightIndex_RR = GetOrDefault<int>(nameof(LightIndex_RR));
        TaxiLightIndex = GetOrDefault<int>(nameof(TaxiLightIndex));
        LeftSirenIndex = GetOrDefault<int>(nameof(LeftSirenIndex));
        RightSirenIndex = GetOrDefault<int>(nameof(RightSirenIndex));
        OtherSirenIndex = GetOrDefault<int>(nameof(OtherSirenIndex));
        WindowsByLOD = GetOrDefault<FWindowSectionInfoContainer[]?>(nameof(WindowsByLOD));
        SectionDamageData = GetOrDefault<FDamageInfoContainer[]?>(nameof(SectionDamageData));
        WindowMIC = GetOrDefault<FPackageIndex?>(nameof(WindowMIC));
    }
}
