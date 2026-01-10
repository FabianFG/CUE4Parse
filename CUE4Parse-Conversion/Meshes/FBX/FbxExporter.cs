// =====================================================================================
// FBX EXPORTER FOR CUE4PARSE-CONVERSION
// =====================================================================================
//
// Exports Unreal Engine skeletal meshes to FBX 7.4 ASCII format with:
// - Skeletal meshes (bones, skin weights, hierarchy)
// - Morph targets (BlendShape deformers for shape keys)
// - Materials (phong shading model with PBR properties)
// - Multiple LOD levels (via MeshExporter)
//
// COORDINATE SYSTEM CONVERSION:
// - Unreal Engine: Z-up, X-forward, Y-right, Right-handed
// - FBX declared as: -Y-up, Z-forward, X-right (creates 180° Z rotation in Blender)
// - Vertex conversion: (X, Y, Z)_unreal → (X, Z, -Y)_fbx
// - Result in Blender: Z-up, -Y-forward (correct orientation)
//
// SCALE CONVERSION:
// - FModel applies SCALE_DOWN_RATIO (0.01) for display
// - We REVERSE this for export (multiply by 100 = SCALE_REVERSE)
//
// =====================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse_Conversion.Meshes.PSK;
using Serilog;

namespace CUE4Parse_Conversion.Meshes.FBX
{
    public class FbxExporter
    {
        private readonly StringBuilder _sb;
        private int _indentLevel;
        private long _nextObjectId = 1000000;
        private long _geometryId;
        private long _modelId;
        private long _armatureId;
        private long _armatureNodeAttrId;
        private readonly Dictionary<string, long> _boneModelIds = new();
        private readonly Dictionary<string, long> _boneNodeAttrIds = new();
        private readonly Dictionary<int, long> _clusterIds = new();
        private long _skinDeformerId;
        private readonly Dictionary<string, long> _blendShapeIds = new();
        private readonly Dictionary<string, long> _blendShapeChannelIds = new();
        private readonly Dictionary<string, long> _shapeGeometryIds = new();
        private readonly Dictionary<int, long> _materialIds = new();
        private readonly Dictionary<string, long> _textureIds = new();
        private long _animStackId;
        private long _animLayerId;
        private readonly Dictionary<string, long> _animCurveNodeIds = new();
        private readonly Dictionary<string, long> _animCurveIds = new();
        private const float SCALE_REVERSE = 100.0f;

        public FbxExporter()
        {
            _sb = new StringBuilder();
            _indentLevel = 0;
        }

        public string ExportStaticMesh(CStaticMeshLod lod, string meshName = "StaticMesh")
        {
            _sb.Clear();
            _indentLevel = 0;

            WriteFbxHeader();
            WriteFbxGlobalSettings();

            _geometryId = GetNextObjectId();
            _modelId = GetNextObjectId();

            WriteStaticObjects(lod, meshName);
            WriteStaticConnections();

            return _sb.ToString();
        }

        public string ExportSkeletalMesh(
            CSkelMeshLod lod,
            List<CSkelMeshBone> bones,
            FPackageIndex[]? morphTargets,
            int lodIndex,
            string meshName = "SkeletalMesh",
            CAnimSequence? animation = null)
        {
            _sb.Clear();
            _indentLevel = 0;
            _boneModelIds.Clear();
            _clusterIds.Clear();
            _blendShapeIds.Clear();
            _blendShapeChannelIds.Clear();
            _shapeGeometryIds.Clear();
            _materialIds.Clear();
            _textureIds.Clear();

            // Debug logging
            Log.Information($"=== FBX EXPORT START ===");
            Log.Information($"Mesh: {meshName}, LOD: {lodIndex}");
            Log.Information($"Bones: {bones.Count}");
            Log.Information($"Vertices: {lod.Verts?.Length ?? 0}");
            Log.Information($"MorphTargets passed: {morphTargets?.Length ?? 0}");

            WriteFbxHeader();
            WriteFbxGlobalSettings();

            _geometryId = GetNextObjectId();
            _modelId = GetNextObjectId();
            _armatureId = GetNextObjectId();
            _skinDeformerId = GetNextObjectId();

            foreach (var bone in bones)
            {
                _boneModelIds[bone.Name.Text] = GetNextObjectId();
            }

            _armatureNodeAttrId = GetNextObjectId();
            foreach (var bone in bones)
            {
                _boneNodeAttrIds[bone.Name.Text] = GetNextObjectId();
            }

            for (int i = 0; i < bones.Count; i++)
            {
                _clusterIds[i] = GetNextObjectId();
            }

            if (morphTargets != null)
            {
                Log.Information($"Processing {morphTargets.Length} morph targets...");
                foreach (var morphTargetRef in morphTargets)
                {
                    var morphTarget = morphTargetRef.Load<UMorphTarget>();
                    if (morphTarget != null)
                    {
                        _blendShapeIds[morphTarget.Name] = GetNextObjectId();
                        _blendShapeChannelIds[morphTarget.Name] = GetNextObjectId();
                        Log.Information($"  Registered morph: {morphTarget.Name}");
                    }
                    else
                    {
                        Log.Warning($"  Failed to load morph target reference");
                    }
                }
                Log.Information($"Total morphs registered: {_blendShapeIds.Count}");
            }
            else
            {
                Log.Warning("No morph targets provided (null)");
            }

            // Register material IDs
            if (lod.Sections != null && lod.Sections.IsValueCreated)
            {
                Log.Information($"Processing {lod.Sections.Value.Length} materials...");
                for (int i = 0; i < lod.Sections.Value.Length; i++)
                {
                    _materialIds[i] = GetNextObjectId();
                    Log.Information($"  Registered material {i}");
                }
            }

            WriteObjects(lod, bones, morphTargets, lodIndex, meshName);
            WriteConnections(bones, morphTargets);

            Log.Information($"=== FBX EXPORT COMPLETE ===");

            return _sb.ToString();
        }

        private void WriteFbxHeader()
        {
            WriteLine("; FBX 7.4.0 project file");
            WriteLine("; Created by CUE4Parse-Conversion FBX Exporter");
            WriteLine("");
            WriteLine("FBXHeaderExtension:  {");
            _indentLevel++;
            WriteLine("FBXHeaderVersion: 1003");
            WriteLine("FBXVersion: 7400");
            WriteLine("Creator: \"CUE4Parse FBX Exporter v1.0\"");
            _indentLevel--;
            WriteLine("}");
            WriteLine("");
        }

        private void WriteFbxGlobalSettings()
        {
            WriteLine("GlobalSettings:  {");
            _indentLevel++;
            WriteLine("Version: 1000");
            WriteLine("Properties70:  {");
            _indentLevel++;
            // -Y-up, +Z-forward, X-right: negate up axis to create 180° Z rotation
            WriteLine("P: \"UpAxis\", \"int\", \"Integer\", \"\",1");  // 1 = Y-axis
            WriteLine("P: \"UpAxisSign\", \"int\", \"Integer\", \"\",-1");  // -1 = negative Y-up
            WriteLine("P: \"FrontAxis\", \"int\", \"Integer\", \"\",2");  // 2 = Z-forward
            WriteLine("P: \"FrontAxisSign\", \"int\", \"Integer\", \"\",1");  // 1 = positive
            WriteLine("P: \"CoordAxis\", \"int\", \"Integer\", \"\",0");  // 0 = X-right
            WriteLine("P: \"CoordAxisSign\", \"int\", \"Integer\", \"\",1");
            WriteLine("P: \"UnitScaleFactor\", \"double\", \"Number\", \"\",1");
            _indentLevel--;
            WriteLine("}");
            _indentLevel--;
            WriteLine("}");
            WriteLine("");
        }

        private void WriteObjects(CSkelMeshLod lod, List<CSkelMeshBone> bones, FPackageIndex[]? morphTargets, int lodIndex, string meshName)
        {
            WriteLine("Objects:  {");
            _indentLevel++;
            WriteGeometry(_geometryId, lod, meshName);
            WriteModel(_modelId, meshName);
            WriteArmature(_armatureId);
            WriteSkeleton(bones);
            WriteNodeAttributes(bones);
            WriteSkinDeformer(lod, bones);
            if (morphTargets != null) WriteMorphTargets(morphTargets, lod, lodIndex);
            if (_materialIds.Count > 0) WriteMaterials(lod);
            _indentLevel--;
            WriteLine("}");
            WriteLine("");
        }

        private void WriteArmature(long id)
        {
            WriteLine($"Model: {id}, \"Model::Armature\", \"Null\" {{");
            _indentLevel++;
            WriteLine("Version: 232");
            WriteLine("Properties70:  {");
            _indentLevel++;
            WriteLine("P: \"InheritType\", \"enum\", \"\", \"\",1");
            _indentLevel--;
            WriteLine("}");
            WriteLine("Shading: Y");
            WriteLine("Culling: \"CullingOff\"");
            _indentLevel--;
            WriteLine("}");
        }

        private void WriteGeometry(long id, CSkelMeshLod lod, string name)
        {
            WriteLine($"Geometry: {id}, \"Geometry::{name}\", \"Mesh\" {{");
            _indentLevel++;
            var verts = lod.Verts!;
            var indices = lod.Indices!.Value;

            WriteLine("Vertices: *" + (verts.Length * 3) + " {");
            _indentLevel++;
            Write("a: ");
            for (int i = 0; i < verts.Length; i++)
            {
                var v = verts[i].Position;
                float x_fbx = v.X * SCALE_REVERSE;
                float y_fbx = v.Z * SCALE_REVERSE;
                float z_fbx = -v.Y * SCALE_REVERSE;
                _sb.Append(FormatFloat(x_fbx) + "," + FormatFloat(y_fbx) + "," + FormatFloat(z_fbx));
                if (i < verts.Length - 1) _sb.Append(",");
            }
            _sb.AppendLine();
            _indentLevel--;
            WriteLine("}");

            WriteLine("PolygonVertexIndex: *" + indices.Length + " {");
            _indentLevel++;
            Write("a: ");
            for (int i = 0; i < indices.Length; i += 3)
            {
                // Original order - PreRotation handles mirroring
                _sb.Append(indices[i] + "," + indices[i + 1] + "," + (-(int)indices[i + 2] - 1));
                if (i < indices.Length - 3) _sb.Append(",");
            }
            _sb.AppendLine();
            _indentLevel--;
            WriteLine("}");

            WriteNormals(verts);
            WriteUVs(verts);

            WriteLine("Layer: 0 {");
            _indentLevel++;
            WriteLine("Version: 100");
            WriteLine("LayerElement:  { Type: \"LayerElementNormal\" TypedIndex: 0 }");
            WriteLine("LayerElement:  { Type: \"LayerElementUV\" TypedIndex: 0 }");
            _indentLevel--;
            WriteLine("}");

            _indentLevel--;
            WriteLine("}");
        }

        private void WriteNormals(CSkelMeshVertex[] verts)
        {
            WriteLine("LayerElementNormal: 0 {");
            _indentLevel++;
            WriteLine("Version: 101");
            WriteLine("Name: \"\"");
            WriteLine("MappingInformationType: \"ByVertice\"");
            WriteLine("ReferenceInformationType: \"Direct\"");
            WriteLine("Normals: *" + (verts.Length * 3) + " {");
            _indentLevel++;
            Write("a: ");
            for (int i = 0; i < verts.Length; i++)
            {
                var n = verts[i].Normal;
                _sb.Append(FormatFloat(n.X) + "," + FormatFloat(n.Z) + "," + FormatFloat(-n.Y));
                if (i < verts.Length - 1) _sb.Append(",");
            }
            _sb.AppendLine();
            _indentLevel--;
            WriteLine("}");
            _indentLevel--;
            WriteLine("}");
        }

        private void WriteUVs(CSkelMeshVertex[] verts)
        {
            WriteLine("LayerElementUV: 0 {");
            _indentLevel++;
            WriteLine("Version: 101");
            WriteLine("Name: \"UVChannel_0\"");
            WriteLine("MappingInformationType: \"ByVertice\"");
            WriteLine("ReferenceInformationType: \"Direct\"");
            WriteLine("UV: *" + (verts.Length * 2) + " {");
            _indentLevel++;
            Write("a: ");
            for (int i = 0; i < verts.Length; i++)
            {
                var uv = verts[i].UV;
                _sb.Append(FormatFloat(uv.U) + "," + FormatFloat(uv.V));
                if (i < verts.Length - 1) _sb.Append(",");
            }
            _sb.AppendLine();
            _indentLevel--;
            WriteLine("}");
            _indentLevel--;
            WriteLine("}");
        }

        private void WriteModel(long id, string name)
        {
            WriteLine($"Model: {id}, \"Model::{name}\", \"Mesh\" {{");
            _indentLevel++;
            WriteLine("Version: 232");
            WriteLine("Shading: T");
            WriteLine("Culling: \"CullingOff\"");
            _indentLevel--;
            WriteLine("}");
        }

        private void WriteSkeleton(List<CSkelMeshBone> bones)
        {
            Log.Information($"Writing {bones.Count} bone models");
            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                var bone = bones[boneIndex];
                var boneId = _boneModelIds[bone.Name.Text];
                WriteLine($"Model: {boneId}, \"Model::{bone.Name.Text}\", \"LimbNode\" {{");
                _indentLevel++;
                WriteLine("Version: 232");
                WriteLine("Properties70:  {");
                _indentLevel++;

                // Convert bone position to FBX coordinate system
                var pos = bone.Position;
                var x = pos.X * SCALE_REVERSE;
                var y = pos.Z * SCALE_REVERSE;
                var z = -pos.Y * SCALE_REVERSE;

                WriteLine($"P: \"Lcl Translation\", \"Lcl Translation\", \"\", \"A\",{FormatFloat(x)},{FormatFloat(y)},{FormatFloat(z)}");

                // Convert rotation quaternion to FBX coordinate system
                var q = bone.Orientation;
                var qx = q.X;
                var qy = q.Z;
                var qz = -q.Y;
                var qw = q.W;

                // Convert quaternion to Euler angles (XYZ order, in degrees)
                var euler = QuaternionToEulerXYZ(qx, qy, qz, qw);
                var rotX = euler.X * (180.0 / Math.PI); // Convert to degrees
                var rotY = euler.Y * (180.0 / Math.PI);
                var rotZ = euler.Z * (180.0 / Math.PI);

                WriteLine($"P: \"Lcl Rotation\", \"Lcl Rotation\", \"\", \"A\",{FormatFloat(rotX)},{FormatFloat(rotY)},{FormatFloat(rotZ)}");
                WriteLine($"P: \"Lcl Scaling\", \"Lcl Scaling\", \"\", \"A\",1,1,1");

                _indentLevel--;
                WriteLine("}");
                WriteLine("Shading: T");
                WriteLine("Culling: \"CullingOff\"");
                _indentLevel--;
                WriteLine("}");
            }
        }

        private void WriteNodeAttributes(List<CSkelMeshBone> bones)
        {
            Log.Information($"Writing NodeAttributes for Armature and {bones.Count} bones");

            // Armature NodeAttribute
            WriteLine($"NodeAttribute: {_armatureNodeAttrId}, \"NodeAttribute::Armature\", \"Null\" {{");
            _indentLevel++;
            WriteLine("TypeFlags: \"Null\"");
            _indentLevel--;
            WriteLine("}");

            // Bone NodeAttributes
            foreach (var bone in bones)
            {
                var nodeAttrId = _boneNodeAttrIds[bone.Name.Text];
                WriteLine($"NodeAttribute: {nodeAttrId}, \"NodeAttribute::{bone.Name.Text}\", \"LimbNode\" {{");
                _indentLevel++;
                WriteLine("TypeFlags: \"Skeleton\"");
                _indentLevel--;
                WriteLine("}");
            }
        }

        private void WriteSkinDeformer(CSkelMeshLod lod, List<CSkelMeshBone> bones)
        {
            WriteLine($"Deformer: {_skinDeformerId}, \"Deformer::Skin\", \"Skin\" {{");
            _indentLevel++;
            WriteLine("Version: 101");
            WriteLine("Link_DeformAcuracy: 50");
            _indentLevel--;
            WriteLine("}");
            var verts = lod.Verts!;

            Log.Information($"=== SKIN DEFORMER VALIDATION ===");
            Log.Information($"Total vertices in mesh: {verts.Length}");
            Log.Information($"Total bones: {bones.Count}");
            Log.Information($"SkinDeformer ID: {_skinDeformerId}");
            Log.Information($"Geometry ID: {_geometryId}");
            Log.Information($"Writing skin deformer for {bones.Count} bones");
            int totalClusters = 0;

            // Compute global transforms for all bones
            var globalTransforms = ComputeGlobalBoneTransforms(bones);

            for (int boneIndex = 0; boneIndex < bones.Count; boneIndex++)
            {
                var influencedVertices = new List<int>();
                var weights = new List<float>();

                for (int vertIndex = 0; vertIndex < verts.Length; vertIndex++)
                {
                    foreach (var influence in verts[vertIndex].Influences)
                    {
                        if (influence.Bone == boneIndex)
                        {
                            influencedVertices.Add(vertIndex);
                            weights.Add(influence.Weight);
                            break;
                        }
                    }
                }

                if (influencedVertices.Count == 0)
                {
                    Log.Warning($"  Bone {boneIndex} ({bones[boneIndex].Name.Text}) has NO influenced vertices - skipping cluster");
                    // Remove cluster ID so we don't create invalid connections later
                    _clusterIds.Remove(boneIndex);
                    continue;
                }

                totalClusters++;

                // Validation: Check vertex indices and weights
                var maxVertIndex = influencedVertices.Max();
                var minVertIndex = influencedVertices.Min();
                var maxWeight = weights.Max();
                var minWeight = weights.Min();

                if (maxVertIndex >= verts.Length)
                {
                    Log.Error($"  Bone {boneIndex} ({bones[boneIndex].Name.Text}): INVALID vertex index {maxVertIndex} (max allowed: {verts.Length - 1})");
                }

                if (minWeight < 0.0f || maxWeight > 1.0f)
                {
                    Log.Warning($"  Bone {boneIndex} ({bones[boneIndex].Name.Text}): Weight range [{minWeight:F3}, {maxWeight:F3}] - may be out of bounds");
                }

                Log.Information($"  Bone {boneIndex} ({bones[boneIndex].Name.Text}): {influencedVertices.Count} verts, indices [{minVertIndex}-{maxVertIndex}], weights [{minWeight:F3}-{maxWeight:F3}]");

                var clusterId = _clusterIds[boneIndex];
                var boneModelId = _boneModelIds[bones[boneIndex].Name.Text];
                var boneName = bones[boneIndex].Name.Text;
                Log.Information($"    ClusterID: {clusterId}, BoneModelID: {boneModelId}");

                WriteLine($"Deformer: {clusterId}, \"SubDeformer::{boneName}\", \"Cluster\" {{");
                _indentLevel++;
                WriteLine("Version: 100");
                WriteLine("UserData: \"\", \"\"");
                WriteLine("Indexes: *" + influencedVertices.Count + " {");
                _indentLevel++;
                WriteLine("a: " + string.Join(",", influencedVertices));
                _indentLevel--;
                WriteLine("} ");
                WriteLine("Weights: *" + weights.Count + " {");
                _indentLevel++;
                WriteLine("a: " + string.Join(",", weights.Select(w => FormatFloat(w))));
                _indentLevel--;
                WriteLine("} ");
                WriteLine("Transform: *16 {");
                _indentLevel++;
                WriteLine("a: 1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1");
                _indentLevel--;
                WriteLine("} ");

                // TransformLink: global bone transform in FBX space
                var transformLink = ConvertMatrixToFbxString(globalTransforms[boneIndex]);
                WriteLine("TransformLink: *16 {");
                _indentLevel++;
                WriteLine("a: " + transformLink);
                _indentLevel--;
                WriteLine("} ");

                // Log first few matrices for validation
                if (boneIndex < 3)
                {
                    Log.Information($"    TransformLink matrix: {transformLink.Substring(0, Math.Min(80, transformLink.Length))}...");
                }

                _indentLevel--;
                WriteLine("}");
            }

            Log.Information($"Total clusters written: {totalClusters}");

            // Validate: Check if all vertices are influenced by at least one bone
            var vertexInfluenceCount = new int[verts.Length];
            var vertexTotalWeight = new float[verts.Length];

            for (int vertIndex = 0; vertIndex < verts.Length; vertIndex++)
            {
                foreach (var influence in verts[vertIndex].Influences)
                {
                    if (influence.Bone >= 0 && influence.Bone < bones.Count)
                    {
                        vertexInfluenceCount[vertIndex]++;
                        vertexTotalWeight[vertIndex] += influence.Weight;
                    }
                }
            }

            var uninfluencedVerts = vertexInfluenceCount.Count(c => c == 0);
            var imperfectWeights = 0;
            for (int i = 0; i < verts.Length; i++)
            {
                if (vertexInfluenceCount[i] > 0 && Math.Abs(vertexTotalWeight[i] - 1.0f) > 0.01f)
                {
                    imperfectWeights++;
                    if (imperfectWeights <= 3)  // Log first 3 examples
                    {
                        Log.Warning($"  Vertex {i}: {vertexInfluenceCount[i]} bones, total weight = {vertexTotalWeight[i]:F3} (should be 1.0)");
                    }
                }
            }

            if (uninfluencedVerts > 0)
            {
                Log.Warning($"Found {uninfluencedVerts} vertices with NO bone influences!");
            }

            if (imperfectWeights > 0)
            {
                Log.Warning($"Found {imperfectWeights} vertices with total weight != 1.0");
            }
            else if (uninfluencedVerts == 0)
            {
                Log.Information($"All vertices properly influenced with normalized weights");
            }
        }

        private (FVector pos, FQuat rot)[] ComputeGlobalBoneTransforms(List<CSkelMeshBone> bones)
        {
            var globalTransforms = new (FVector pos, FQuat rot)[bones.Count];

            Log.Information("=== COMPUTING GLOBAL BONE TRANSFORMS ===");
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                Log.Information($"Bone {i}: '{bone.Name.Text}', Parent: {bone.ParentIndex}");
                Log.Information($"  Local Pos: ({bone.Position.X:F3}, {bone.Position.Y:F3}, {bone.Position.Z:F3})");
                Log.Information($"  Local Rot: ({bone.Orientation.X:F3}, {bone.Orientation.Y:F3}, {bone.Orientation.Z:F3}, {bone.Orientation.W:F3})");

                if (bone.ParentIndex >= 0 && bone.ParentIndex < i)
                {
                    // Combine with parent transform
                    var parentTransform = globalTransforms[bone.ParentIndex];

                    // Rotate local position by parent rotation
                    var rotatedPos = RotateVectorByQuat(bone.Position, parentTransform.rot);
                    globalTransforms[i].pos = new FVector(
                        parentTransform.pos.X + rotatedPos.X,
                        parentTransform.pos.Y + rotatedPos.Y,
                        parentTransform.pos.Z + rotatedPos.Z
                    );

                    // Combine rotations
                    globalTransforms[i].rot = MultiplyQuats(parentTransform.rot, bone.Orientation);

                    Log.Information($"  Global Pos: ({globalTransforms[i].pos.X:F3}, {globalTransforms[i].pos.Y:F3}, {globalTransforms[i].pos.Z:F3})");
                    Log.Information($"  Global Rot: ({globalTransforms[i].rot.X:F3}, {globalTransforms[i].rot.Y:F3}, {globalTransforms[i].rot.Z:F3}, {globalTransforms[i].rot.W:F3})");
                }
                else
                {
                    // Root bone - use local transform as global
                    globalTransforms[i] = (bone.Position, bone.Orientation);
                    Log.Information($"  ROOT BONE - Global = Local");
                }
            }

            return globalTransforms;
        }

        private FVector RotateVectorByQuat(FVector v, FQuat q)
        {
            // v' = q * v * q^-1
            var qx = q.X;
            var qy = q.Y;
            var qz = q.Z;
            var qw = q.W;

            var ix = qw * v.X + qy * v.Z - qz * v.Y;
            var iy = qw * v.Y + qz * v.X - qx * v.Z;
            var iz = qw * v.Z + qx * v.Y - qy * v.X;
            var iw = -qx * v.X - qy * v.Y - qz * v.Z;

            return new FVector(
                ix * qw + iw * -qx + iy * -qz - iz * -qy,
                iy * qw + iw * -qy + iz * -qx - ix * -qz,
                iz * qw + iw * -qz + ix * -qy - iy * -qx
            );
        }

        private FQuat MultiplyQuats(FQuat q1, FQuat q2)
        {
            return new FQuat(
                q1.W * q2.X + q1.X * q2.W + q1.Y * q2.Z - q1.Z * q2.Y,
                q1.W * q2.Y + q1.Y * q2.W + q1.Z * q2.X - q1.X * q2.Z,
                q1.W * q2.Z + q1.Z * q2.W + q1.X * q2.Y - q1.Y * q2.X,
                q1.W * q2.W - q1.X * q2.X - q1.Y * q2.Y - q1.Z * q2.Z
            );
        }

        private string ConvertMatrixToFbxString((FVector pos, FQuat rot) transform)
        {
            // Convert position to FBX coordinate system
            var x = transform.pos.X * SCALE_REVERSE;
            var y = transform.pos.Z * SCALE_REVERSE;
            var z = -transform.pos.Y * SCALE_REVERSE;

            // Convert rotation quaternion to FBX coordinate system
            var q = transform.rot;
            var qx = q.X;
            var qy = q.Z;
            var qz = -q.Y;
            var qw = q.W;

            // Convert quaternion to rotation matrix
            var xx = qx * qx;
            var yy = qy * qy;
            var zz = qz * qz;
            var xy = qx * qy;
            var xz = qx * qz;
            var yz = qy * qz;
            var wx = qw * qx;
            var wy = qw * qy;
            var wz = qw * qz;

            var m00 = 1 - 2 * (yy + zz);
            var m01 = 2 * (xy - wz);
            var m02 = 2 * (xz + wy);
            var m10 = 2 * (xy + wz);
            var m11 = 1 - 2 * (xx + zz);
            var m12 = 2 * (yz - wx);
            var m20 = 2 * (xz - wy);
            var m21 = 2 * (yz + wx);
            var m22 = 1 - 2 * (xx + yy);

            // Build 4x4 transform matrix (column-major order for FBX)
            return $"{FormatFloat(m00)},{FormatFloat(m10)},{FormatFloat(m20)},0," +
                   $"{FormatFloat(m01)},{FormatFloat(m11)},{FormatFloat(m21)},0," +
                   $"{FormatFloat(m02)},{FormatFloat(m12)},{FormatFloat(m22)},0," +
                   $"{FormatFloat(x)},{FormatFloat(y)},{FormatFloat(z)},1";
        }

        private void WriteMorphTargets(FPackageIndex[] morphTargets, CSkelMeshLod lod, int lodIndex)
        {
            foreach (var morphTargetRef in morphTargets)
            {
                var morphTarget = morphTargetRef.Load<UMorphTarget>();
                if (morphTarget?.MorphLODModels == null || morphTarget.MorphLODModels.Length <= lodIndex) continue;

                var morphModel = morphTarget.MorphLODModels[lodIndex];
                var deltas = new List<(int idx, FVector delta)>();

                for (int i = 0; i < morphModel.Vertices.Length; i++)
                {
                    var delta = morphModel.Vertices[i];
                    if (delta.SourceIdx < lod.Verts!.Length)
                    {
                        deltas.Add(((int)delta.SourceIdx, delta.PositionDelta));
                    }
                }

                if (deltas.Count == 0) continue;

                Log.Information($"Morph '{morphTarget.Name}': {deltas.Count} deltas");

                var blendShapeId = _blendShapeIds[morphTarget.Name];
                var channelId = _blendShapeChannelIds[morphTarget.Name];
                var shapeGeomId = GetNextObjectId();
                _shapeGeometryIds[morphTarget.Name] = shapeGeomId;

                WriteLine($"Deformer: {blendShapeId}, \"Deformer::\", \"BlendShape\" {{ Version: 100 }}");
                WriteLine($"Deformer: {channelId}, \"SubDeformer::{morphTarget.Name}\", \"BlendShapeChannel\" {{");
                _indentLevel++;
                WriteLine("Version: 100");
                WriteLine("DeformPercent: 0");
                WriteLine($"Shape: {shapeGeomId}");
                _indentLevel--;
                WriteLine("}");

                WriteLine($"Geometry: {shapeGeomId}, \"Geometry::{morphTarget.Name}\", \"Shape\" {{");
                _indentLevel++;
                WriteLine("Version: 100");
                WriteLine("Indexes: *" + deltas.Count + " { a: " + string.Join(",", deltas.Select(d => d.idx)) + " }");
                
                WriteLine("Vertices: *" + (deltas.Count * 3) + " {");
                _indentLevel++;
                Write("a: ");
                for (int i = 0; i < deltas.Count; i++)
                {
                    var d = deltas[i].delta;
                    _sb.Append(FormatFloat(d.X * SCALE_REVERSE) + "," + FormatFloat(d.Z * SCALE_REVERSE) + "," + FormatFloat(-d.Y * SCALE_REVERSE));
                    if (i < deltas.Count - 1) _sb.Append(",");
                }
                _sb.AppendLine();
                _indentLevel--;
                WriteLine("}");
                WriteLine("Normals: *" + (deltas.Count * 3) + " { a: " + string.Join(",", Enumerable.Repeat("0,0,0", deltas.Count)) + " }");
                _indentLevel--;
                WriteLine("}");
            }
        }

        private void WriteConnections(List<CSkelMeshBone> bones, FPackageIndex[]? morphTargets)
        {
            Log.Information($"=== WRITING CONNECTIONS ===");
            Log.Information($"Connection: Geometry({_geometryId}) -> Model({_modelId})");
            Log.Information($"Connection: Armature({_armatureId}) -> Root(0)");
            Log.Information($"Connection: SkinDeformer({_skinDeformerId}) -> Geometry({_geometryId})");

            WriteLine("Connections:  {");
            _indentLevel++;
            WriteLine($"C: \"OO\",{_geometryId},{_modelId}");
            WriteLine($"C: \"OO\",{_armatureId},0");
            WriteLine($"C: \"OO\",{_skinDeformerId},{_geometryId}");

            // NodeAttribute connections
            WriteLine($";NodeAttribute::Armature, Model::Armature");
            WriteLine($"C: \"OO\",{_armatureNodeAttrId},{_armatureId}");

            int clusterConnectionCount = 0;
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                var boneId = _boneModelIds[bone.Name.Text];
                var nodeAttrId = _boneNodeAttrIds[bone.Name.Text];

                // NodeAttribute -> Model connection for this bone
                WriteLine($";NodeAttribute::{bone.Name.Text}, Model::{bone.Name.Text}");
                WriteLine($"C: \"OO\",{nodeAttrId},{boneId}");

                if (bone.ParentIndex >= 0 && bone.ParentIndex < bones.Count)
                {
                    WriteLine($"C: \"OO\",{boneId},{_boneModelIds[bones[bone.ParentIndex].Name.Text]}");
                }
                else
                {
                    // Root bones connect to Armature, not to Mesh
                    WriteLine($"C: \"OO\",{boneId},{_armatureId}");
                }

                if (_clusterIds.ContainsKey(i))
                {
                    var clusterId = _clusterIds[i];
                    WriteLine($"C: \"OO\",{clusterId},{_skinDeformerId}");
                    WriteLine($"C: \"OO\",{boneId},{clusterId}");

                    if (clusterConnectionCount < 5)  // Log first 5 for verification
                    {
                        Log.Information($"  Bone {i} ({bone.Name.Text}): Cluster({clusterId}) -> SkinDeformer({_skinDeformerId}), BoneModel({boneId}) -> Cluster({clusterId})");
                    }
                    clusterConnectionCount++;
                }
            }

            Log.Information($"Total cluster connections written: {clusterConnectionCount}");

            if (morphTargets != null)
            {
                foreach (var morphTargetRef in morphTargets)
                {
                    var morphTarget = morphTargetRef.Load<UMorphTarget>();
                    if (morphTarget != null && _blendShapeIds.ContainsKey(morphTarget.Name))
                    {
                        WriteLine($"C: \"OO\",{_blendShapeIds[morphTarget.Name]},{_geometryId}");
                        WriteLine($"C: \"OO\",{_blendShapeChannelIds[morphTarget.Name]},{_blendShapeIds[morphTarget.Name]}");
                        if (_shapeGeometryIds.ContainsKey(morphTarget.Name))
                        {
                            WriteLine($"C: \"OO\",{_shapeGeometryIds[morphTarget.Name]},{_blendShapeChannelIds[morphTarget.Name]}");
                        }
                    }
                }
            }

            // Material connections
            foreach (var materialEntry in _materialIds)
            {
                int materialIndex = materialEntry.Key;
                long materialId = materialEntry.Value;
                WriteLine($";Material::{materialIndex} -> Model::{_modelId}");
                WriteLine($"C: \"OO\",{materialId},{_modelId}");
            }

            _indentLevel--;
            WriteLine("}");
        }

        private void WriteStaticObjects(CStaticMeshLod lod, string meshName)
        {
            WriteLine("Objects:  {");
            _indentLevel++;
            WriteStaticGeometry(_geometryId, lod, meshName);
            WriteModel(_modelId, meshName);
            _indentLevel--;
            WriteLine("}");
            WriteLine("");
        }

        private void WriteStaticGeometry(long id, CStaticMeshLod lod, string name)
        {
            WriteLine($"Geometry: {id}, \"Geometry::{name}\", \"Mesh\" {{");
            _indentLevel++;
            var verts = lod.Verts!;
            var indices = lod.Indices!.Value;

            WriteLine("Vertices: *" + (verts.Length * 3) + " {");
            _indentLevel++;
            Write("a: ");
            for (int i = 0; i < verts.Length; i++)
            {
                var v = verts[i].Position;
                float x_fbx = v.X * SCALE_REVERSE;
                float y_fbx = v.Z * SCALE_REVERSE;
                float z_fbx = -v.Y * SCALE_REVERSE;
                _sb.Append(FormatFloat(x_fbx) + "," + FormatFloat(y_fbx) + "," + FormatFloat(z_fbx));
                if (i < verts.Length - 1) _sb.Append(",");
            }
            _sb.AppendLine();
            _indentLevel--;
            WriteLine("}");

            WriteLine("PolygonVertexIndex: *" + indices.Length + " {");
            _indentLevel++;
            Write("a: ");
            for (int i = 0; i < indices.Length; i += 3)
            {
                // Original order - PreRotation handles mirroring
                _sb.Append(indices[i] + "," + indices[i + 1] + "," + (-(int)indices[i + 2] - 1));
                if (i < indices.Length - 3) _sb.Append(",");
            }
            _sb.AppendLine();
            _indentLevel--;
            WriteLine("}");

            WriteStaticNormals(verts);
            WriteStaticUVs(verts);

            WriteLine("Layer: 0 {");
            _indentLevel++;
            WriteLine("Version: 100");
            WriteLine("LayerElement:  { Type: \"LayerElementNormal\" TypedIndex: 0 }");
            WriteLine("LayerElement:  { Type: \"LayerElementUV\" TypedIndex: 0 }");
            _indentLevel--;
            WriteLine("}");

            _indentLevel--;
            WriteLine("}");
        }

        private void WriteStaticNormals(CMeshVertex[] verts)
        {
            WriteLine("LayerElementNormal: 0 {");
            _indentLevel++;
            WriteLine("Version: 101");
            WriteLine("Name: \"\"");
            WriteLine("MappingInformationType: \"ByVertice\"");
            WriteLine("ReferenceInformationType: \"Direct\"");
            WriteLine("Normals: *" + (verts.Length * 3) + " {");
            _indentLevel++;
            Write("a: ");
            for (int i = 0; i < verts.Length; i++)
            {
                var n = verts[i].Normal;
                _sb.Append(FormatFloat(n.X) + "," + FormatFloat(n.Z) + "," + FormatFloat(-n.Y));
                if (i < verts.Length - 1) _sb.Append(",");
            }
            _sb.AppendLine();
            _indentLevel--;
            WriteLine("}");
            _indentLevel--;
            WriteLine("}");
        }

        private void WriteStaticUVs(CMeshVertex[] verts)
        {
            WriteLine("LayerElementUV: 0 {");
            _indentLevel++;
            WriteLine("Version: 101");
            WriteLine("Name: \"UVChannel_0\"");
            WriteLine("MappingInformationType: \"ByVertice\"");
            WriteLine("ReferenceInformationType: \"Direct\"");
            WriteLine("UV: *" + (verts.Length * 2) + " {");
            _indentLevel++;
            Write("a: ");
            for (int i = 0; i < verts.Length; i++)
            {
                var uv = verts[i].UV;
                _sb.Append(FormatFloat(uv.U) + "," + FormatFloat(uv.V));
                if (i < verts.Length - 1) _sb.Append(",");
            }
            _sb.AppendLine();
            _indentLevel--;
            WriteLine("}");
            _indentLevel--;
            WriteLine("}");
        }

        private void WriteStaticConnections()
        {
            WriteLine("Connections:  {");
            _indentLevel++;
            WriteLine($"C: \"OO\",{_geometryId},{_modelId}");
            _indentLevel--;
            WriteLine("}");
        }

        private long GetNextObjectId() => _nextObjectId++;
        private void WriteLine(string line) { _sb.Append('\t', _indentLevel); _sb.AppendLine(line); }
        private void Write(string text) { _sb.Append('\t', _indentLevel); _sb.Append(text); }
        private (double X, double Y, double Z) QuaternionToEulerXYZ(double qx, double qy, double qz, double qw)
        {
            // Convert quaternion to Euler angles (XYZ rotation order)
            // Reference: https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles

            double sinr_cosp = 2 * (qw * qx + qy * qz);
            double cosr_cosp = 1 - 2 * (qx * qx + qy * qy);
            double roll = Math.Atan2(sinr_cosp, cosr_cosp);

            double sinp = 2 * (qw * qy - qz * qx);
            double pitch;
            if (Math.Abs(sinp) >= 1)
                pitch = Math.CopySign(Math.PI / 2, sinp); // Use 90 degrees if out of range
            else
                pitch = Math.Asin(sinp);

            double siny_cosp = 2 * (qw * qz + qx * qy);
            double cosy_cosp = 1 - 2 * (qy * qy + qz * qz);
            double yaw = Math.Atan2(siny_cosp, cosy_cosp);

            return (roll, pitch, yaw);
        }

        private void WriteMaterials(CSkelMeshLod lod)
        {
            if (lod.Sections == null || !lod.Sections.IsValueCreated)
                return;

            Log.Information($"=== WRITING MATERIALS ===");

            for (int i = 0; i < lod.Sections.Value.Length; i++)
            {
                var section = lod.Sections.Value[i];
                var materialId = _materialIds[i];
                var materialName = section.MaterialName ?? $"Material_{i}";

                Log.Information($"Writing material {i}: {materialName}");

                WriteLine($"Material: {materialId}, \"Material::{materialName}\", \"\" {{");
                _indentLevel++;
                WriteLine("Version: 102");
                WriteLine("ShadingModel: \"phong\"");
                WriteLine("MultiLayer: 0");
                WriteLine("Properties70:  {");
                _indentLevel++;

                // Default material properties
                WriteLine("P: \"Diffuse\", \"Vector3D\", \"Vector\", \"\",0.8,0.8,0.8");
                WriteLine("P: \"DiffuseColor\", \"Color\", \"\", \"A\",0.8,0.8,0.8");
                WriteLine("P: \"Emissive\", \"Vector3D\", \"Vector\", \"\",0,0,0");
                WriteLine("P: \"EmissiveColor\", \"Color\", \"\", \"A\",0,0,0");
                WriteLine("P: \"Ambient\", \"Vector3D\", \"Vector\", \"\",0.2,0.2,0.2");
                WriteLine("P: \"AmbientColor\", \"Color\", \"\", \"A\",0.2,0.2,0.2");
                WriteLine("P: \"Specular\", \"Vector3D\", \"Vector\", \"\",0.2,0.2,0.2");
                WriteLine("P: \"SpecularColor\", \"Color\", \"\", \"A\",0.2,0.2,0.2");
                WriteLine("P: \"Shininess\", \"double\", \"Number\", \"\",20");
                WriteLine("P: \"ShininessExponent\", \"Number\", \"\", \"A\",20");
                WriteLine("P: \"Opacity\", \"double\", \"Number\", \"\",1");
                WriteLine("P: \"Reflectivity\", \"double\", \"Number\", \"\",0");

                _indentLevel--;
                WriteLine("}");
                _indentLevel--;
                WriteLine("}");
                WriteLine("");
            }
        }

        private string FormatFloat(float value) => value.ToString("F6", CultureInfo.InvariantCulture);
        private string FormatFloat(double value) => value.ToString("F6", CultureInfo.InvariantCulture);
    }
}
