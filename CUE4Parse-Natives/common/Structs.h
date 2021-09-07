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