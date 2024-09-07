using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.RigVM;

public class FRigVMMemoryContainer
{
    public bool bUseNameMap;
    public ERigVMMemoryType MemoryType;
    public FRigVMRegister[] Registers;
    public FRigVMRegisterOffset[] RegisterOffsets;
    public string[] ScriptStructPaths;
    public ulong TotalBytes;

    public FRigVMMemoryContainer(FAssetArchive Ar)
    {
        bUseNameMap = Ar.ReadBoolean();
        MemoryType = Ar.Read<ERigVMMemoryType>();
        Registers = Ar.ReadArray(() => new FRigVMRegister(Ar));
        RegisterOffsets = Ar.ReadArray(() => new FRigVMRegisterOffset(Ar));
        ScriptStructPaths = Ar.ReadArray(Ar.ReadFString);
        TotalBytes = Ar.Read<ulong>();

        object? view; 
        foreach (var register in Registers)
        {
            if (register.ElementCount == 0 && !register.IsDynamic()) continue;

            if (!register.IsDynamic() || !register.IsNestedDynamic())
            {
                view = register.Type switch
                {
                    ERigVMRegisterType.Plain => Ar.ReadArray<byte>(),
                    ERigVMRegisterType.Name => Ar.ReadArray(Ar.ReadFName),
                    ERigVMRegisterType.Struct or ERigVMRegisterType.String => Ar.ReadArray(Ar.ReadFString),
                    _ => null
                };
            }
            else
            {
                view = new List<object>();
                for (var sliceIndex = 0; sliceIndex < register.SliceCount; sliceIndex++)
                {
                    object? cView = register.Type switch
                    {
                        ERigVMRegisterType.Plain => Ar.ReadArray<byte>(),
                        ERigVMRegisterType.Name => Ar.ReadArray(Ar.ReadFName),
                        ERigVMRegisterType.Struct or ERigVMRegisterType.String => Ar.ReadArray(Ar.ReadFString),
                        _ => null
                    };
                    ((List<object>) view).Add(cView);
                }
            }
            register.View = view;
        }
    }
}
