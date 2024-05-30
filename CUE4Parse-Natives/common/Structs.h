#pragma once

struct FVector
{
    float X;
    float Y;
    float Z;

    FVector() : X(0), Y(0), Z(0)
    {
    }

    FVector(float x, float y, float z)
        : X(x), Y(y), Z(z)
    {
    }

    FVector operator-(FVector v)
    {
        return FVector(X - v.X, Y - v.Y, Z - v.Z);
    }

    FVector operator+(FVector v)
    {
        return FVector(X + v.X, Y + v.Y, Z + v.Z);
    }

    float Distance(FVector v)
    {
        return ((X - v.X) * (X - v.X) +
            (Y - v.Y) * (Y - v.Y) +
            (Z - v.Z) * (Z - v.Z));
    }
};

struct FRotator
{
    float Pitch;
    float Yaw;
    float Roll;

    FRotator() : Pitch(0), Yaw(0), Roll(0)
    {
    }

    FRotator(float pitch, float yaw, float roll)
        : Pitch(pitch), Yaw(yaw), Roll(roll)
    {
    }
};

struct FQuat
{
    float X, Y, Z, W;

    FQuat() : X(0), Y(0), Z(0), W(0)
    {
    }

    FQuat(float x, float y, float z, float w)
        : X(x), Y(y), Z(z), W(w)
    {
    }
};

struct FTransform
{
    FQuat Rotation;
    FVector Translation;
    FVector Scale3D;
};

struct FACLTransform final : public FTransform
{
    RTM_FORCE_INLINE void RTM_SIMD_CALL SetRotationRaw(rtm::quatf_arg0 Rotation_)
    {
        rtm::quat_store(Rotation_, &Rotation.X);
    }

    RTM_FORCE_INLINE void RTM_SIMD_CALL SetTranslationRaw(rtm::vector4f_arg0 Translation_)
    {
        rtm::vector_store3(Translation_, &Translation.X);
    }

    RTM_FORCE_INLINE void RTM_SIMD_CALL SetScale3DRaw(rtm::vector4f_arg0 Scale3D_)
    {
        rtm::vector_store3(Scale3D_, &Scale3D.X);
    }
};

struct FTrackToSkeletonMap
{
    int32_t BoneTreeIndex;
};
