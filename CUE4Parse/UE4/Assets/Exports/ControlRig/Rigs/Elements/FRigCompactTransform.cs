using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;

namespace CUE4Parse.UE4.Assets.Exports.ControlRig.Rigs.Elements;

public class FRigCompactTransform
{
    public FTransform Transform;

    public FRigCompactTransform(FTransform transform)
    {
        Transform = transform;
    }

    public void Load(FArchive Ar, FRigHierarchySerializationSettings  inSettings)
    {
        if (inSettings.ControlRigVersion < FControlRigObjectVersion.Type.RigHierarchyCompactTransformSerialization)
        {
            Transform = new FTransform(Ar);
            return;
        }

        var state = Ar.Read<ERepresentation>();

        Transform = FTransform.Identity;
        FVector position;
        FQuat rotation;
        FVector scale;
        float scaleX;

        switch (state)
        {
            case ERepresentation.Float_Zero_Identity_One:
                break;
            case ERepresentation.Float_Zero_Identity_Uniform:
            {
                scaleX = Ar.Read<float>();
                Transform.SetScale3D(new FVector(scaleX));
                break;
            }
            case ERepresentation.Float_Zero_Identity_NonUniform:
            {
                scale = Ar.Read<FVector>();
                Transform.SetScale3D(scale);
                break;
            }
            case ERepresentation.Float_Zero_Quat_One:
            {
                rotation = Ar.Read<FQuat>();
                Transform.SetRotation(rotation);
                break;
            }
            case ERepresentation.Float_Zero_Quat_Uniform:
            {
                rotation = Ar.Read<FQuat>();
                scaleX = Ar.Read<float>();

                Transform.SetRotation(rotation);
                Transform.SetScale3D(new FVector(scaleX));

                break;
            }
            case ERepresentation.Float_Zero_Quat_NonUniform:
            {
                rotation = Ar.Read<FQuat>();
                scale = Ar.Read<FVector>();

                Transform.SetRotation(rotation);
                Transform.SetScale3D(scale);

                break;
            }
            case ERepresentation.Float_Position_Identity_One:
            {
                position = Ar.Read<FVector>();
                Transform.SetLocation(position);
                break;
            }
            case ERepresentation.Float_Position_Identity_Uniform:
            {
                position = Ar.Read<FVector>();
                scaleX = Ar.Read<float>();

                Transform.SetLocation(position);
                Transform.SetScale3D(new FVector(scaleX));

                break;
            }
            case ERepresentation.Float_Position_Identity_NonUniform:
            {
                position = Ar.Read<FVector>();
                scale = Ar.Read<FVector>();

                Transform.SetLocation(position);
                Transform.SetScale3D(scale);

                break;
            }
            case ERepresentation.Float_Position_Quat_One:
            {
                position = Ar.Read<FVector>();
                rotation = Ar.Read<FQuat>();

                Transform.SetLocation(position);
                Transform.SetRotation(rotation);

                break;
            }
            case ERepresentation.Float_Position_Quat_Uniform:
            {
                position = Ar.Read<FVector>();
                rotation = Ar.Read<FQuat>();
                scaleX = Ar.Read<float>();

                Transform.SetLocation(position);
                Transform.SetRotation(rotation);
                Transform.SetScale3D(new FVector(scaleX));

                break;
            }
            case ERepresentation.Float_Position_Quat_NonUniform:
            {
                position = Ar.Read<FVector>();
                rotation = Ar.Read<FQuat>();
                scale = Ar.Read<FVector>();

                Transform.SetLocation(position);
                Transform.SetRotation(rotation);
                Transform.SetScale3D(scale);

                break;
            }
            case ERepresentation.Double_Complete:
            default:
            {
                Transform = new FTransform(Ar);
                break;
            }
        }
    }
}

public enum ERepresentation : byte
{
    Float_Zero_Identity_One = 0,
    Float_Zero_Identity_Uniform = 1,
    Float_Zero_Identity_NonUniform = 2,
    Float_Zero_Quat_One = 3,
    Float_Zero_Quat_Uniform = 4,
    Float_Zero_Quat_NonUniform = 5,
    Float_Position_Identity_One = 6,
    Float_Position_Identity_Uniform = 7,
    Float_Position_Identity_NonUniform = 8,
    Float_Position_Quat_One = 9,
    Float_Position_Quat_Uniform = 10,
    Float_Position_Quat_NonUniform = 11,
    Double_Complete = 12,
    Last = Double_Complete,
    Max = Last + 1
}
