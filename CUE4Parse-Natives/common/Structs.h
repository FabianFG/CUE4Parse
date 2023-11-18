#pragma once


#define uint unsigned int


template<typename T>
struct TArray
{
    T* Data;
    int Count;
    size_t Max;

    TArray(TArray& other)
    {
        Data = new T[other.Count];
        Count = other.Count;
        Max = other.Count;
        for (int i = 0; i < Count; i++)
        {
            Data[i] = other.Data[i];
        }
    }

    TArray() : Data(nullptr), Count(0), Max(0)
    {
    }

    TArray(T* data, int count): Data(data), Count(count), Max(count)
    {
    }

    ~TArray()
    {
        // do nothing thanks
        //Clear();
    }

    T* GetData()
    {
        return Data;
    } 

    T& operator[](int index)
    {
        return Data[index];
    }

    TArray& operator=(const TArray& other)
    {
        if (this != &other)
        {
            delete[] Data;
            Data = new T[other.Count];
            Count = other.Count;
            Max = other.Count;
            for (int i = 0; i < Count; i++)
            {
                Data[i] = other.Data[i];
            }
        }
        return *this;
    }

    const T& operator[](int index) const
    {
        return Data[index];
    }

    void Add(T item)
    {
        if (Data == nullptr)
        {
			Data = new T[1];
			Count = 1;
			Max = 1;
		}
        else if (Count == Max)
        {
			Max *= 2;
			T* newData = new T[Max];
            for (int i = 0; i < Count; i++)
            {
				newData[i] = Data[i];
			}
			delete[] Data;
			Data = newData;
		}
#pragma warning(suppress: 6386) // not working ??
		Data[Count] = item;
		Count++;
	}


    void Reserve(size_t count) // i hate this. remove this please
    {
        if (Data == nullptr)
        {
            Data = new T[count];
            Count = 0;
            Max = count;
        }
        else if (Max < count)
        {
            Max = count;
            T* newData = new T[Max];
            for (int i = 0; i < Count; i++)
            {
                newData[i] = Data[i];
            }
            delete[] Data;
            Data = newData;
        }
    }

    void AddUninitialized(int count)
    {
        if (Data == nullptr)
        {
            Data = new T[count];
            Count = count;
            Max = count;
        }
        else if (Count + count > Max)
        {
            Max = Count + count;
            T* newData = new T[Max];
            for (int i = 0; i < Count; i++)
            {
                newData[i] = Data[i];
            }
            delete[] Data;
            Data = newData;
            Count += count;
        }
        else
        {
            Count += count;
        }
    }

    void Clear()
    {
        delete[] Data;
        Data = nullptr;
        Count = 0;
        Max = 0;
    }

};


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


struct FLinearColor
{
    float R;
    float G;
    float B;
    float A;
};

struct FUVFloat
{
    float U, V;
};

struct FColor
{
    unsigned char B, G, R, A;

    FLinearColor ReinterpretAsLinear() const // TODO: check if this is correct
    {
        return FLinearColor{ R / 255.f, G / 255.f, B / 255.f, A / 255.f };
    }
};
