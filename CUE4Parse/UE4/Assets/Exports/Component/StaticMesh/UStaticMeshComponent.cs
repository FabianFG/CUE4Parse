using System;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;

public class UStaticMeshComponent : UMeshComponent
{
    public FStaticMeshComponentLODInfo[] LODData;
    public FPackageIndex? MeshPaintTextureCooked;
    protected FPackageIndex? StaticMesh;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        if (Ar.Position == validPos) return;
        if (Ar.Game is EGame.GAME_Borderlands3) Ar.ReadBoolean();
        LODData = Ar.ReadArray(() => new FStaticMeshComponentLODInfo(Ar));
        if (Ar.Game is EGame.GAME_SuicideSquad)
        {
            var count = Ar.Read<int>();
            for (var i = 0; i < count; i++)
            {
                Ar.SkipFixedArray(12);
                var idk = Ar.Read<int>();
                Ar.Position += idk.Align(32) >> 3;
                Ar.SkipFixedArray(2);
            }

        }

        if (FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.MeshPaintTextureUsesEditorOnly)
        {
            var bSerializeAsCookedData = Ar.ReadBoolean();
            if (bSerializeAsCookedData)
                MeshPaintTextureCooked = new FPackageIndex(Ar);
        }
    }

    public virtual FPackageIndex GetStaticMesh()
    {
        if (StaticMesh != null)
            return StaticMesh;
        var mesh = new FPackageIndex();
        var current = this;
        while (true)
        {
            if (current is null) break;
            mesh = current.GetOrDefault("StaticMesh", new FPackageIndex());
            if (!mesh.IsNull || current.Template == null)
                break;
            current = current.Template.Load<UStaticMeshComponent>();
        }
        StaticMesh = mesh;
        return mesh;
    }

    private WeakReference<UStaticMesh> StaticMeshRef;
    public UStaticMesh? GetLoadedStaticMesh()
    {
        if (StaticMeshRef?.TryGetTarget(out var mesh) == true)
            return mesh;
        var meshIdx = GetStaticMesh();
        if (meshIdx.IsNull)
            return null;
        mesh = meshIdx.Load<UStaticMesh>();
        StaticMeshRef = new WeakReference<UStaticMesh>(mesh);
        return mesh;
    }

    public bool SetStaticMeshIfNull(FPackageIndex mesh)
    {
        if (GetStaticMesh().IsNull)
        {
            SetStaticMesh(mesh);
            return true;
        }
        return false;
    }

    public void SetStaticMesh(FPackageIndex mesh)
    {
        PropertyUtil.Set(this, "StaticMesh", mesh);
        StaticMesh = mesh;
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        if (LODData is { Length: <= 0 }) return;
        writer.WritePropertyName("LODData");
        serializer.Serialize(writer, LODData);
    }
}

public class UBaseBuildingStaticMeshComponent : UStaticMeshComponent;
