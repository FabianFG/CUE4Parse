using CUE4Parse.UE4.Assets.Readers;

namespace CUE4Parse.UE4.Objects.RigVM
{
    public class FRigVMMemoryContainer
    {
        public bool bUseNameMap;
        public ERigVMMemoryType MemoryType;
        public FRigVMRegister[] Registers;
        public FRigVMRegisterOffset[] RegisterOffsets;
        public string[] ScriptStructPaths;
        public ulong TotalBytes;
        public object View; // View can have dynamically different data, so it's just object here

        public FRigVMMemoryContainer(FAssetArchive Ar)
        {
            bUseNameMap = Ar.ReadBoolean();
            MemoryType = Ar.Read<ERigVMMemoryType>();
            Registers = Ar.ReadArray(() => new FRigVMRegister(Ar));
            RegisterOffsets = Ar.ReadArray(() => new FRigVMRegisterOffset(Ar));
            ScriptStructPaths = Ar.ReadArray(Ar.ReadFString);
            TotalBytes = Ar.Read<ulong>();

            foreach (var register in Registers)
            {
                if (register.ElementCount == 0 && !register.IsDynamic()) continue;

                if (!register.IsDynamic() || !register.IsNestedDynamic())
                {
                    switch (register.Type)
                    {
                        case ERigVMRegisterType.Plain:
                        {
                            View = Ar.ReadArray<byte>();
                            break;
                        }
                        case ERigVMRegisterType.Name:
                        {
                            View = Ar.ReadArray(Ar.ReadFName);
                            break;
                        }
                        case ERigVMRegisterType.Struct:
                        case ERigVMRegisterType.String:
                        {
                            View = Ar.ReadArray(Ar.ReadFString);
                            break;
                        }
                    }
                }
                else
                {
                    for (var sliceIndex = 0; sliceIndex < register.SliceCount; sliceIndex++)
                    {
                        switch (register.Type)
                        {
                            case ERigVMRegisterType.Plain:
                            {
                                View = Ar.ReadArray<byte>();
                                break;
                            }
                            case ERigVMRegisterType.Name:
                            {
                                View = Ar.ReadArray(Ar.ReadFName);
                                break;
                            }
                            case ERigVMRegisterType.Struct:
                            case ERigVMRegisterType.String:
                            {
                                View = Ar.ReadArray(Ar.ReadFString);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    public enum ERigVMMemoryType : byte
    {
        Work, // Mutable state
        Literal, // Const / fixed state
        External, // Unowned external memory
        Invalid
    }
}