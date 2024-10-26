using System;
using CUE4Parse.UE4.Assets.Readers;
using FImageSize = CUE4Parse.UE4.Objects.Core.Math.TIntVector2<ushort>;

namespace CUE4Parse.UE4.Assets.Exports.CustomizableObject.Mutable.Images;

public class Image : IMutablePtr
{
    public int Version;
    public FImageDataStorage DataStorage;
    
    public bool IsBroken { get; set; }

    public Image(FAssetArchive Ar)
    {
        Version = Ar.Read<int>();

        if (Version == -1)
        {
            IsBroken = true;
            return;
        }

        if (Version > 4)
            throw new NotSupportedException($"Mutable Image Version '{Version}' is currently not supported.");

        if (Version <= 3)
        {
            DataStorage = new FImageDataStorage
            {
                ImageSize = Ar.Read<FImageSize>(),
                NumLODs = Ar.Read<byte>(),
                ImageFormat = Ar.Read<EImageFormat>(),
                Buffers = Ar.ReadArray(Ar.ReadArray<byte>)
            };
        }
        else
        {
            DataStorage = new FImageDataStorage(Ar);
        }
        
        Ar.Read<byte>(); // m_flags
    }
}