using System;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Core.Compression;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects;

[Flags]
public enum EFlags
{
    /** No flags are set */
    None						= 0,
    /** Is the data actually virtualized or not? */
    IsVirtualized				= 1 << 0,
    /** Does the package have access to a .upayload file? */
    HasPayloadSidecarFile		= 1 << 1,
    /** The bulkdata object is currently referencing a payload saved under old bulkdata formats */
    ReferencesLegacyFile		= 1 << 2,
    /** The legacy file being referenced is stored with Zlib compression format */
    LegacyFileIsCompressed		= 1 << 3,
    /** The payload should not have compression applied to it. It is assumed that the payload is already
        in some sort of compressed format, see the compression documentation above for more details. */
    DisablePayloadCompression	= 1 << 4,
    /** The legacy file being referenced derived its key from guid and it should be replaced with a key-from-hash when saved */
    LegacyKeyWasGuidDerived		= 1 << 5,
    /** (Transient) The Guid has been registered with the BulkDataRegistry */
    HasRegistered				= 1 << 6,
    /** (Transient) The BulkData object is a copy used only to represent the id and payload; it does not communicate with the BulkDataRegistry, and will point DDC jobs toward the original BulkData */
    IsTornOff					= 1 << 7,
    /** The bulkdata object references a payload stored in a WorkspaceDomain file  */
    ReferencesWorkspaceDomain	= 1 << 8,
    /** The payload is stored in a package trailer, so the bulkdata object will have to poll the trailer to find the payload offset */
    StoredInPackageTrailer		= 1 << 9,
    /** The bulkdata object was cooked. */
    IsCooked					= 1 << 10,
    /** (Transient) The package owning the bulkdata has been detached from disk and we can no longer load from it */
    WasDetached					= 1 << 11
}

public class FEditorBulkData
{
    public EFlags Flags { get; set; }
    public FGuid BulkDataId { get; set; }
    public FSHAHash PayloadContentId { get; set; }
    public long PayloadSize { get; set; }
    public long OffsetInFile { get; set; }
    public FCompressedBuffer Payload { get; set; }

    public FEditorBulkData(FAssetArchive Ar)
    {
        Flags = Ar.Read<EFlags>();
        BulkDataId = Ar.Read<FGuid>();
        PayloadContentId = new FSHAHash(Ar);
        PayloadSize = Ar.Read<long>();
        if (Flags.HasFlag(EFlags.StoredInPackageTrailer))
        {
            if (Ar.Owner is Package package && package.Trailer is not null)
            {
                OffsetInFile = package.Trailer.FindPayloadOffsetInFile(PayloadContentId);
            }
            else
            {
                Log.Warning("BulkData marked as stored in package trailer, but package has no trailer");
                Payload = new FCompressedBuffer();
                return;
            }
        }
        else
        {
            OffsetInFile = Ar.Read<long>();
        }

        if (OffsetInFile == -1)
        {
            Payload = new FCompressedBuffer();
            return;
        }

        var savedPos = Ar.Position;
        try
        {
            Ar.Position = OffsetInFile;
            Payload = new FCompressedBuffer(Ar);
        } 
        catch (Exception e) 
        { 
            Log.Error(e, "Failed to read to EditorBulkData payload at offset {OffsetInFile}", OffsetInFile); 
            Payload = new FCompressedBuffer(); 
            return; 
        }
        finally
        {
            Ar.Position = savedPos;
        }
    }
}
