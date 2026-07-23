using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.UEFormat.Natives;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.PhysicsEngine;
using CUE4Parse.UE4.Objects.UObject;

namespace CUE4Parse_Conversion.Meshes.UEFormat;

public static class UEModel
{
    public static byte[] Export(string name, string objectPath, CStaticMesh mesh, FPackageIndex bodySetupLazy, ExporterOptions options)
    {
        using var pin = new NativePinScope();
        var lods = BuildLods(mesh.LODs, options, pin, skeletal: false, morphTargets: null);
        var collisions = BuildCollisions(bodySetupLazy, pin);

        var desc = new UEFormatModelDesc
        {
            Lods = pin.PinArray(lods),
            LodCount = lods.Length,
            Skeleton = IntPtr.Zero,
            Collisions = pin.PinArray(collisions),
            CollisionCount = collisions.Length,
        };
        return UEFormatNativeSave.SaveModel(ref desc, name, objectPath, options, pin);
    }

    public static byte[] Export(
        string name,
        string objectPath,
        CSkeletalMesh mesh,
        FPackageIndex[]? morphTargets,
        FPackageIndex[] sockets,
        FPackageIndex skeletonLazy,
        FPackageIndex physicsAssetLazy,
        ExporterOptions options)
    {
        using var pin = new NativePinScope();
        var lods = BuildLods(mesh.LODs, options, pin, skeletal: true, morphTargets);
        var skeleton = BuildSkeleton(skeletonLazy.Load<USkeleton>(), mesh.RefSkeleton, sockets, [], pin);
        var skeletonPtr = pin.PinStruct(ref skeleton);

        var desc = new UEFormatModelDesc
        {
            Lods = pin.PinArray(lods),
            LodCount = lods.Length,
            Skeleton = skeletonPtr,
            Collisions = IntPtr.Zero,
            CollisionCount = 0,
        };
        return UEFormatNativeSave.SaveModel(ref desc, name, objectPath, options, pin);
    }

    public static byte[] Export(
        string name,
        string objectPath,
        USkeleton skeleton,
        List<CSkelMeshBone> bones,
        FPackageIndex[] sockets,
        FVirtualBone[] virtualBones,
        ExporterOptions options)
    {
        using var pin = new NativePinScope();
        var skeletonDesc = BuildSkeleton(skeleton, bones, sockets, virtualBones, pin);
        var skeletonPtr = pin.PinStruct(ref skeletonDesc);

        var desc = new UEFormatModelDesc
        {
            Lods = IntPtr.Zero,
            LodCount = 0,
            Skeleton = skeletonPtr,
            Collisions = IntPtr.Zero,
            CollisionCount = 0,
        };
        return UEFormatNativeSave.SaveModel(ref desc, name, objectPath, options, pin);
    }

    private static UEFormatModelLodDesc[] BuildLods<TLod>(
        IReadOnlyList<TLod> sourceLods,
        ExporterOptions options,
        NativePinScope pin,
        bool skeletal,
        FPackageIndex[]? morphTargets)
        where TLod : CBaseMeshLod
    {
        var lods = new List<UEFormatModelLodDesc>();
        for (var lodIdx = 0; lodIdx < sourceLods.Count; lodIdx++)
        {
            var lod = sourceLods[lodIdx];
            if (lod.SkipLod) continue;

            IReadOnlyList<CMeshVertex> verts;
            int morphLodIndex = 0;
            CSkelMeshVertex[]? skelVerts = null;

            if (lod is CSkelMeshLod skelLod)
            {
                verts = skelLod.Verts ?? [];
                skelVerts = skelLod.Verts;
                morphLodIndex = skelLod.LODIndex;
            }
            else if (lod is CStaticMeshLod staticLod)
            {
                verts = staticLod.Verts ?? [];
            }
            else
            {
                continue;
            }

            lods.Add(BuildLodGeometry(
                $"LOD{lodIdx}",
                verts,
                lod.Indices!.Value,
                lod.VertexColors,
                lod.ExtraVertexColors,
                lod.Sections!.Value,
                lod.ExtraUV!.Value,
                skeletal ? skelVerts : null,
                skeletal ? morphTargets : null,
                morphLodIndex,
                pin));

            if (options.LodFormat == ELodFormat.FirstLod) break;
        }

        return lods.ToArray();
    }

    private static UEFormatModelLodDesc BuildLodGeometry(
        string name,
        IReadOnlyList<CMeshVertex> verts,
        uint[] indices,
        FColor[]? vertexColors,
        CVertexColor[]? extraVertexColors,
        CMeshSection[] sections,
        FMeshUVFloat[][] extraUVs,
        CSkelMeshVertex[]? skelVerts,
        FPackageIndex[]? morphTargets,
        int morphLodIndex,
        NativePinScope pin)
    {
        var vertices = new UEFormatVector[verts.Count];
        var normals = new UEFormatNormal[verts.Count];
        var tangents = new UEFormatVector[verts.Count];
        var mainUvs = new UEFormatMeshUV[verts.Count];

        for (var i = 0; i < verts.Count; i++)
        {
            var vert = verts[i];
            vertices[i] = UEFormatNativeSave.ToVector(vert.Position);

            var normal = (FVector)vert.Normal;
            normal /= MathF.Sqrt(normal | normal);
            normals[i] = new UEFormatNormal
            {
                BinormalSign = vert.Normal.W,
                Normal = UEFormatNativeSave.ToVector(normal),
            };

            var tangent = (FVector)vert.Tangent;
            tangent.Normalize();
            tangents[i] = UEFormatNativeSave.ToVector(tangent);
            mainUvs[i] = UEFormatNativeSave.ToUv(vert.UV);
        }

        var texCoords = new List<UEFormatTexCoordEntryDesc>
        {
            new()
            {
                Name = pin.AllocUtf8("UV0"),
                Uvs = pin.PinArray(mainUvs),
                UvCount = mainUvs.Length,
            }
        };

        for (var uvIdx = 0; uvIdx < extraUVs.Length; uvIdx++)
        {
            var uvSet = extraUVs[uvIdx];
            var mapped = new UEFormatMeshUV[uvSet.Length];
            for (var i = 0; i < uvSet.Length; i++)
                mapped[i] = UEFormatNativeSave.ToUv(uvSet[i]);

            texCoords.Add(new UEFormatTexCoordEntryDesc
            {
                Name = pin.AllocUtf8($"UV{uvIdx + 1}"),
                Uvs = pin.PinArray(mapped),
                UvCount = mapped.Length,
            });
        }

        var texCoordArray = texCoords.ToArray();

        var colorDescs = new List<UEFormatVertexColorDesc>();
        if (vertexColors is { Length: > 0 })
        {
            var colors = new UEFormatColor[vertexColors.Length];
            for (var i = 0; i < vertexColors.Length; i++)
                colors[i] = UEFormatNativeSave.ToColor(vertexColors[i]);

            colorDescs.Add(new UEFormatVertexColorDesc
            {
                Name = pin.AllocUtf8("COL0"),
                Data = pin.PinArray(colors),
                Count = colors.Length,
            });
        }

        if (extraVertexColors is { Length: > 0 })
        {
            foreach (var extra in extraVertexColors)
            {
                if (extra.ColorData is null) continue;
                var colors = new UEFormatColor[extra.ColorData.Length];
                for (var i = 0; i < extra.ColorData.Length; i++)
                    colors[i] = UEFormatNativeSave.ToColor(extra.ColorData[i]);

                colorDescs.Add(new UEFormatVertexColorDesc
                {
                    Name = pin.AllocUtf8(extra.Name),
                    Data = pin.PinArray(colors),
                    Count = colors.Length,
                });
            }
        }

        var colorArray = colorDescs.ToArray();

        var materials = new UEFormatMaterialDesc[sections.Length];
        for (var i = 0; i < sections.Length; i++)
        {
            var section = sections[i];
            materials[i] = new UEFormatMaterialDesc
            {
                MaterialName = pin.AllocUtf8(section.Material?.Name.Text ?? section.MaterialName ?? string.Empty),
                MaterialPath = pin.AllocUtf8(section.Material?.GetPathName() ?? string.Empty),
                FirstIndex = section.FirstIndex,
                NumFaces = section.NumFaces,
            };
        }

        UEFormatWeightDesc[]? weights = null;
        if (skelVerts is not null)
        {
            var weightList = new List<UEFormatWeightDesc>();
            for (var vertexIndex = 0; vertexIndex < skelVerts.Length; vertexIndex++)
            {
                foreach (var influence in skelVerts[vertexIndex].Influences)
                {
                    weightList.Add(new UEFormatWeightDesc
                    {
                        Bone = influence.Bone,
                        VertexIndex = vertexIndex,
                        Weight = influence.Weight,
                    });
                }
            }

            weights = weightList.ToArray();
        }

        UEFormatMorphTargetDesc[]? morphDescs = null;
        if (morphTargets is { Length: > 0 })
        {
            var morphList = new List<UEFormatMorphTargetDesc>();
            foreach (var morphTarget in morphTargets)
            {
                var morph = morphTarget.Load<UMorphTarget>();
                if (morph?.MorphLODModels is null ||
                    morphLodIndex >= morph.MorphLODModels.Length ||
                    morph.MorphLODModels[morphLodIndex].Vertices.Length == 0)
                {
                    continue;
                }

                var morphLod = morph.MorphLODModels[morphLodIndex];
                var morphData = new UEFormatMorphDataDesc[morphLod.Vertices.Length];
                for (var i = 0; i < morphLod.Vertices.Length; i++)
                {
                    var delta = morphLod.Vertices[i];
                    morphData[i] = new UEFormatMorphDataDesc
                    {
                        PositionDelta = UEFormatNativeSave.ToVector(delta.PositionDelta),
                        TangentZDelta = UEFormatNativeSave.ToVector(delta.TangentZDelta),
                        VertexIndex = delta.SourceIdx,
                    };
                }

                morphList.Add(new UEFormatMorphTargetDesc
                {
                    MorphName = pin.AllocUtf8(morph.Name),
                    MorphData = pin.PinArray(morphData),
                    MorphDataCount = morphData.Length,
                });
            }

            morphDescs = morphList.ToArray();
        }

        return new UEFormatModelLodDesc
        {
            Name = pin.AllocUtf8(name),
            Vertices = pin.PinArray(vertices),
            VertexCount = vertices.Length,
            Normals = pin.PinArray(normals),
            NormalCount = normals.Length,
            Tangents = pin.PinArray(tangents),
            TangentCount = tangents.Length,
            TextureCoordinates = pin.PinArray(texCoordArray),
            TextureCoordinateCount = texCoordArray.Length,
            Indices = pin.PinArray(indices),
            IndexCount = indices.Length,
            VertexColors = pin.PinArray(colorArray),
            VertexColorCount = colorArray.Length,
            Materials = pin.PinArray(materials),
            MaterialCount = materials.Length,
            Weights = pin.PinArray(weights),
            WeightCount = weights?.Length ?? 0,
            MorphTargets = pin.PinArray(morphDescs),
            MorphTargetCount = morphDescs?.Length ?? 0,
        };
    }

    private static UEFormatModelSkeletonDesc BuildSkeleton(
        USkeleton? skeleton,
        List<CSkelMeshBone> bones,
        FPackageIndex[] sockets,
        FVirtualBone[] virtualBones,
        NativePinScope pin)
    {
        var boneDescs = new UEFormatBoneDesc[bones.Count];
        for (var i = 0; i < bones.Count; i++)
        {
            var bone = bones[i];
            boneDescs[i] = new UEFormatBoneDesc
            {
                BoneName = pin.AllocUtf8(bone.Name.Text),
                ParentIndex = bone.ParentIndex,
                Position = UEFormatNativeSave.ToVector(bone.Position),
                Orientation = UEFormatNativeSave.ToQuat(bone.Orientation),
            };
        }

        var socketList = new List<UEFormatSocketDesc>();
        foreach (var socketObject in sockets)
        {
            var socket = socketObject.Load<USkeletalMeshSocket>();
            if (socket is null) continue;

            socketList.Add(new UEFormatSocketDesc
            {
                SocketName = pin.AllocUtf8(socket.SocketName.Text),
                BoneName = pin.AllocUtf8(socket.BoneName.Text),
                RelativeLocation = UEFormatNativeSave.ToVector(socket.RelativeLocation),
                RelativeRotation = UEFormatNativeSave.ToQuat(socket.RelativeRotation.Quaternion()),
                RelativeScale = UEFormatNativeSave.ToVector(socket.RelativeScale),
            });
        }

        var socketArray = socketList.ToArray();

        var virtualBoneDescs = new UEFormatVirtualBoneDesc[virtualBones.Length];
        for (var i = 0; i < virtualBones.Length; i++)
        {
            var vb = virtualBones[i];
            virtualBoneDescs[i] = new UEFormatVirtualBoneDesc
            {
                SourceBoneName = pin.AllocUtf8(vb.SourceBoneName.Text),
                TargetBoneName = pin.AllocUtf8(vb.TargetBoneName.Text),
                VirtualBoneName = pin.AllocUtf8(vb.VirtualBoneName.Text),
            };
        }

        return new UEFormatModelSkeletonDesc
        {
            MetadataPath = pin.AllocUtf8(skeleton?.GetPathName() ?? string.Empty),
            Bones = pin.PinArray(boneDescs),
            BoneCount = boneDescs.Length,
            Sockets = pin.PinArray(socketArray),
            SocketCount = socketArray.Length,
            VirtualBones = pin.PinArray(virtualBoneDescs),
            VirtualBoneCount = virtualBoneDescs.Length,
        };
    }

    private static UEFormatConvexCollisionDesc[] BuildCollisions(FPackageIndex bodySetupLazy, NativePinScope pin)
    {
        if (!bodySetupLazy.TryLoad<UBodySetup>(out var bodySetup) ||
            bodySetup.AggGeom?.ConvexElems is not { } convexElems)
            return [];

        var collisions = new UEFormatConvexCollisionDesc[convexElems.Length];
        for (var i = 0; i < convexElems.Length; i++)
        {
            var convex = convexElems[i];
            var vertices = new UEFormatVector[convex.VertexData.Length];
            for (var v = 0; v < convex.VertexData.Length; v++)
                vertices[v] = UEFormatNativeSave.ToVector(convex.VertexData[v]);

            collisions[i] = new UEFormatConvexCollisionDesc
            {
                Name = pin.AllocUtf8(convex.Name.Text),
                VertexData = pin.PinArray(vertices),
                VertexCount = vertices.Length,
                IndexData = pin.PinArray(convex.IndexData),
                IndexCount = convex.IndexData.Length,
            };
        }

        return collisions;
    }
}
