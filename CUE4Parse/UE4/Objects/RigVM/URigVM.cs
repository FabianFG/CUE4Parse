using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Objects.RigVM;

public class URigVM : Assets.Exports.UObject
{
    public uint CachedVMHash;
    public string? ExecuteContextPath;
    public FRigVMPropertyPathDescription[]? ExternalPropertyPathDescriptions;
    public FName[]? FunctionNamesStorage;
    public FRigVMByteCode? ByteCodeStorage;
    public FRigVMParameter[]? Parameters;
    public Dictionary<FRigVMOperand, FRigVMOperand[]>? OperandToDebugRegisters;
    public Dictionary<string, FSoftObjectPath>? UserDefinedStructGuidToPathName;
    public Dictionary<string, FSoftObjectPath>? UserDefinedEnumToPathName;
    public FRigVMMemoryContainer? WorkMemoryStorage;
    public FRigVMMemoryStorageStruct? LiteralMemoryStorage;
    public FRigVMMemoryStorageStruct? DefaultWorkMemoryStorage;
    public FRigVMMemoryStorageStruct? DefaultDebugMemoryStorage;
    public FRigVMMemoryContainer? LiteralMemoryStorageOld;
    public FRigVMMemoryContainer? DefaultWorkMemoryStorageOld;
    public FRigVMMemoryContainer? DefaultDebugMemoryStorageOld;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        if (FAnimObjectVersion.Get(Ar) < FAnimObjectVersion.Type.StoreMarkerNamesOnSkeleton) return;
        //base.Deserialize(Ar, validPos); //maybe

        if (FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.BeforeCustomVersionWasAdded)
        {
            int RigVMUClassBasedStorageDefine = 1;
            if (FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.RigVMMemoryStorageObject)
                RigVMUClassBasedStorageDefine = Ar.Read<int>();

            if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.RigVMExternalExecuteContextStruct
                && FUE5MainStreamObjectVersion.Get(Ar) >= FUE5MainStreamObjectVersion.Type.RigVMSerializeExecuteContextStruct)
            {
                // Context is now external to the VM, just serializing the string to keep compatibility
                ExecuteContextPath = Ar.ReadFString();
            }

            if (RigVMUClassBasedStorageDefine == 1)
            {
                WorkMemoryStorage = new FRigVMMemoryContainer(Ar);
                LiteralMemoryStorageOld = new FRigVMMemoryContainer(Ar);
                FunctionNamesStorage = Ar.ReadArray(Ar.ReadFName);
                ByteCodeStorage = new FRigVMByteCode(Ar);
                Parameters = Ar.ReadArray(() => new FRigVMParameter(Ar));

                if (FUE5MainStreamObjectVersion.Get(Ar) < FUE5MainStreamObjectVersion.Type.RigVMCopyOpStoreNumBytes) return;

                if (FUE5ReleaseStreamObjectVersion.Get(Ar) >= FUE5ReleaseStreamObjectVersion.Type.RigVMSaveDebugMapInGraphFunctionData)
                {
                    var num = Ar.Read<int>();
                    OperandToDebugRegisters = [];
                    for (var i = 0; i < num; i++)
                    {
                        OperandToDebugRegisters[Ar.Read<FRigVMOperand>()] = Ar.ReadArray(Ar.Read<FRigVMOperand>);
                    }
                }
            }

            if (RigVMUClassBasedStorageDefine != 0) return;
        }

        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.AddedVMHashChecks)
        {
            CachedVMHash = Ar.Read<uint>();
        }

        ExternalPropertyPathDescriptions = Ar.ReadArray(() => new FRigVMPropertyPathDescription(Ar));
        FunctionNamesStorage = Ar.ReadArray(Ar.ReadFName);
        ByteCodeStorage = new FRigVMByteCode(Ar);
        Parameters = Ar.ReadArray(() => new FRigVMParameter(Ar));

        if (FUE5ReleaseStreamObjectVersion.Get(Ar) >= FUE5ReleaseStreamObjectVersion.Type.RigVMSaveDebugMapInGraphFunctionData ||
            FFortniteMainBranchObjectVersion.Get(Ar) >= FFortniteMainBranchObjectVersion.Type.RigVMSaveDebugMapInGraphFunctionData)
        {
            var num = Ar.Read<int>();
            OperandToDebugRegisters = [];
            for (var i = 0; i < num; i++)
            {
                OperandToDebugRegisters[Ar.Read<FRigVMOperand>()] = Ar.ReadArray(Ar.Read<FRigVMOperand>);
            }
        }

        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.VMStoringUserDefinedStructMap &&
            FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.HostStoringUserDefinedData)
        {
            UserDefinedStructGuidToPathName = [];
            int num = Ar.Read<int>();
            for (var i = 0; i < num; i++)
            {
                UserDefinedStructGuidToPathName[Ar.ReadFString()] = Ar.Read<FSoftObjectPath>();
            }
        }

        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.VMMemoryStorageStructSerialized &&
            FRigVMObjectVersion.Get(Ar) < FRigVMObjectVersion.Type.HostStoringUserDefinedData)
        {
            UserDefinedEnumToPathName = [];
            int num = Ar.Read<int>();
            for (var i = 0; i < num; i++)
            {
                UserDefinedEnumToPathName[Ar.ReadFString()] = Ar.Read<FSoftObjectPath>();
            }
        }

        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.VMMemoryStorageStructSerialized)
        {
            LiteralMemoryStorage = new FRigVMMemoryStorageStruct(Ar);
        }

        if (FRigVMObjectVersion.Get(Ar) >= FRigVMObjectVersion.Type.VMMemoryStorageDefaultsGeneratedAtVM)
        {
            DefaultWorkMemoryStorage = new FRigVMMemoryStorageStruct(Ar);
            DefaultDebugMemoryStorage = new FRigVMMemoryStorageStruct(Ar);
        }
    }

    protected internal override void WriteJson(JsonWriter writer, JsonSerializer serializer)
    {
        base.WriteJson(writer, serializer);

        writer.WritePropertyName("WorkMemoryStorage");
        serializer.Serialize(writer, WorkMemoryStorage);

        writer.WritePropertyName("LiteralMemoryStorage");
        serializer.Serialize(writer, LiteralMemoryStorage);

        writer.WritePropertyName("ByteCodeStorage");
        serializer.Serialize(writer, ByteCodeStorage);

        writer.WritePropertyName("Parameters");
        serializer.Serialize(writer, Parameters);
    }
}
