using System;
using System.IO;
using System.Text;

namespace CUE4Parse.GameTypes.ABI.Encryption.Aes;

public static class ABILua
{
    private static readonly byte[] _luaDecryptionKey = Encoding.ASCII.GetBytes("hotbeaf\0");
    private static readonly byte[] _luaHeader = Encoding.ASCII.GetBytes("\x1bLua");
    private static readonly byte[] _luacData = [0x19, 0x93, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] _luacInt = [0x78, 0x56, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] _luacNum = BitConverter.GetBytes(370.5);
    private static readonly byte _luaVersion = 0x54;
    private static readonly int _luaIntegerSize = 8;
    private static readonly int _luaNumberSize = 8;

    public static byte[] DecryptLuaBytecode(byte[] bytes)
    {
        var outBytes = DecryptStream(bytes);
        return outBytes;
    }

    // Note:
    // luac header is intentionally encrypted with incorrect lua values just to obfuscate it further
    // therefore I'm writing back correct ones manually
    private static byte[] DecryptStream(byte[] encryptedFile)
    {
        using var Ar = new MemoryStream(encryptedFile);

        Ar.Write(_luaHeader, 0, 4);

        Ar.WriteByte(_luaVersion);

        ReadAndDecryptBlock(Ar, 1); // LUAC_FORMAT

        Ar.Write(_luacData, 0, 6);

        ReadAndDecryptBlock(Ar, 1); // sizeof(Instruction)
        ReadAndDecryptBlock(Ar, 1); // sizeof(lua_Integer)
        ReadAndDecryptBlock(Ar, 1); // sizeof(lua_Number)

        Ar.Write(_luacInt, 0, 8);

        Ar.Write(_luacNum, 0, 8);

        ReadAndDecryptBlock(Ar, 1);

        ProcessFunction(Ar);

        return Ar.ToArray();
    }

    private static void ProcessFunction(Stream Ar)
    {
        ulong sourceSizePlusOne = UndumpSize(Ar);
        if (sourceSizePlusOne != 0)
        {
            ulong contentLen = sourceSizePlusOne - 1;
            if (contentLen > int.MaxValue)
                throw new NotSupportedException("Huge source string");
            ReadAndDecryptBlock(Ar, (int) contentLen);
        }

        UndumpSize(Ar); // linedefined
        UndumpSize(Ar); // lastlinedefined

        ReadAndDecryptBlock(Ar, 1); // numparams
        ReadAndDecryptBlock(Ar, 1); // is_vararg
        ReadAndDecryptBlock(Ar, 1); // maxstacksize

        int sizecode = (int) UndumpSize(Ar);
        if (sizecode > int.MaxValue)
            throw new NotSupportedException("sizecode too large");
        if (sizecode > 0)
            ReadAndDecryptBlock(Ar, checked(sizecode * 4));

        int sizek = (int) UndumpSize(Ar);
        for (int i = 0; i < sizek; i++)
        {
            int type = ReadByte(Ar);
            switch (type)
            {
                case 3: // int
                    ReadAndDecryptBlock(Ar, _luaIntegerSize);
                    break;
                case 19: // float
                    ReadAndDecryptBlock(Ar, _luaNumberSize);
                    break;
                case 4: // long string
                case 20: // short string
                    ulong strlenPlusOne = UndumpSize(Ar);
                    if (strlenPlusOne != 0)
                    {
                        int strlen = checked((int) (strlenPlusOne - 1));
                        ReadAndDecryptBlock(Ar, strlen);
                    }
                    break;
                default:
                    break;
            }
        }

        int sizeupvalues = (int) UndumpSize(Ar);
        for (int i = 0; i < sizeupvalues; i++)
        {
            ReadByte(Ar); // instack
            ReadByte(Ar); // idx
            ReadByte(Ar); // kind
        }

        int sizep = (int) UndumpSize(Ar);
        if (sizep > int.MaxValue)
            throw new NotSupportedException("sizep too large");
        for (int i = 0; i < sizep; i++)
        {
            ProcessFunction(Ar);
        }

        ProcessDebug(Ar);
    }

    private static void ProcessDebug(Stream Ar)
    {
        ulong sizelineinfo = UndumpSize(Ar);
        if (sizelineinfo > int.MaxValue)
            throw new NotSupportedException("lineinfo too large");
        if (sizelineinfo > 0)
            ReadAndDecryptBlock(Ar, (int) sizelineinfo);

        ulong sizeabslineinfo = UndumpSize(Ar);
        if (sizeabslineinfo > int.MaxValue)
            throw new NotSupportedException("abslineinfo too large");
        for (ulong i = 0; i < sizeabslineinfo; i++)
        {
            UndumpSize(Ar); // pc
            UndumpSize(Ar); // line
        }

        ulong sizelocvars = UndumpSize(Ar);
        if (sizelocvars > int.MaxValue)
            throw new NotSupportedException("locvars too large");
        for (ulong i = 0; i < sizelocvars; i++)
        {
            ulong varnameLenPlus1 = UndumpSize(Ar);
            if (varnameLenPlus1 != 0)
            {
                ulong varlen = varnameLenPlus1 - 1;
                if (varlen > int.MaxValue)
                    throw new NotSupportedException("varname too large");
                ReadAndDecryptBlock(Ar, (int) varlen);
            }
            UndumpSize(Ar); // startpc
            UndumpSize(Ar); // endpc
        }

        ulong sizeupvalues = UndumpSize(Ar);
        if (sizeupvalues > int.MaxValue)
            throw new NotSupportedException("debug upvalues too large");
        for (ulong i = 0; i < sizeupvalues; i++)
        {
            ulong nameLenPlus1 = UndumpSize(Ar);
            if (nameLenPlus1 != 0)
            {
                ulong namelen = nameLenPlus1 - 1;
                if (namelen > int.MaxValue)
                    throw new NotSupportedException("upvalue name too large");
                ReadAndDecryptBlock(Ar, (int) namelen);
            }
        }
    }

    private static void ReadAndDecryptBlock(Stream Ar, int len)
    {
        if (len == 0)
            return;

        var enc = new byte[len];
        int read = Ar.Read(enc, 0, len);
        if (read != len)
            throw new EndOfStreamException();

        var dec = DecryptBlock(enc);

        Ar.Position -= len;
        Ar.Write(dec, 0, dec.Length);
    }

    private static byte[] DecryptBlock(byte[] enc)
    {
        var outb = new byte[enc.Length];
        byte prev = _luaDecryptionKey[0];
        int mask = _luaDecryptionKey.Length;
        for (int i = 0; i < enc.Length; i++)
        {
            byte k = (byte) ((_luaDecryptionKey[i % mask] | prev) & 0xFF);
            byte plain = (byte) (enc[i] ^ k);
            outb[i] = plain;
            prev = plain;
        }
        return outb;
    }

    private static byte ReadByte(Stream Ar)
    {
        int encByte = Ar.ReadByte();
        if (encByte < 0)
            throw new EndOfStreamException();

        byte dec = (byte) (encByte ^ _luaDecryptionKey[0]);
        Ar.Position -= 1;
        Ar.WriteByte(dec);
        return dec;
    }

    private static ulong UndumpSize(Stream Ar)
    {
        ulong value = 0;
        int readCount = 0;
        const int maxBytes = 16;

        while (true)
        {
            int encByte = Ar.ReadByte();
            if (encByte < 0)
                throw new EndOfStreamException("Unexpected end of stream while reading varint");

            readCount++;
            if (readCount > maxBytes)
                throw new InvalidOperationException("Varint too long or corrupted");

            byte dec = (byte) encByte;

            Ar.Position -= 1;
            Ar.WriteByte(dec);

            value = value << 7 | (uint) (dec & 0x7F);

            if ((dec & 0x80) != 0)
                break;
        }

        return value;
    }
}
