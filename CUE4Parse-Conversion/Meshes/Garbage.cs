using CUE4Parse.Utils;
using CUE4Parse_Conversion.Materials;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse_Conversion.Meshes.Filmbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Writers;


namespace CUE4Parse_Conversion.Meshes
{
    public class FBX
    {
        IntPtr ScenePtr = IntPtr.Zero;

        public FBX(string name, CStaticMeshLod lod, List<MaterialExporter2>? materialExports, ExporterOptions options)
        {
            if (!CUE4ParseNatives.IsFeatureAvailable("FBX"))
                throw new NotSupportedException("FBX Unsupported");

            var nLod = new StaticMeshLod
            {
                NumVertices = lod.NumVerts,
                NumTexCoords = lod.NumTexCoords,
                Sections = new TArray<MeshSection>(lod.Sections.Value.Select(x => new MeshSection(x)).ToArray()),
                ExtraUVs = new TArray<TArray<FMeshUVFloat>>(lod.ExtraUV.Value.Select(x => new TArray<FMeshUVFloat>(x)).ToArray()),
                VertexColors = new TArray<FColor>(lod.VertexColors ?? Array.Empty<FColor>()),
                Indices = new TArray<uint>(lod.Indices.Value.Indices16.Length > 0 ? lod.Indices.Value.Indices16.Select(x => (uint)x).ToArray() : lod.Indices.Value.Indices32),
                Vertices = new TArray<MeshVertex>(lod.Verts.Select(x => new MeshVertex(x)).ToArray())
            };

            ScenePtr = CreateStaticMesh(name, nLod, false);

            if (ScenePtr == IntPtr.Zero)
                throw new Exception("Failed to create FBX Scene");
        }

        // can support FBX 5/6/7 Binary & ASCII, Collada, DXF, OBJ, 3DS
        // see FbxExporter for more info
        public void Save(EMeshFormat meshFormat, FArchiveWriter Ar)
        {
            IntPtr file_data = SaveScene(ScenePtr, 7700);
            byte[] file_bytes;
            unsafe
            {
                TArray<byte> file = *(TArray<byte>*)file_data;
                file_bytes = new byte[file.Count];
                Marshal.Copy(file.Data, file_bytes, 0, file.Count);
            }
            Ar.Write(file_bytes);
            // TODO: Free file_data
        }

        // Call Native CreateScene Function
        [DllImport("CUE4Parse-Natives")]
        public static extern IntPtr CreateScene();

        // Call Native DLLEXPORT void* CreateStaticMesh(char* name, StaticMeshLod lod, bool bWeldVerts)
        [DllImport("CUE4Parse-Natives")]
        public static extern IntPtr CreateStaticMesh([MarshalAs(UnmanagedType.LPStr)] string name, StaticMeshLod lod, bool bWeldVerts);

        //DLLEXPORT void* SaveScene(void* Scene, int FBXFileVersion /*7700*/)
        [DllImport("CUE4Parse-Natives")]
        public static extern IntPtr SaveScene(IntPtr Scene, int FBXFileVersion);
    }
}
