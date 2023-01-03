using System.Numerics;

namespace CUE4Parse.GameTypes.ACE7.Encryption;

public class ACE7XORKey
{
    public int NameKey;
    public int Offset;
    public int pk1;
    public int pk2;

    private static int CalcNameKey(string fname)
    {
        fname = fname.ToUpper();
        var num = 0;
        for (var i = 0; i < fname.Length; i++)
        {
            int num2 = (byte) fname[i];
            num ^= num2;
            num2 = num * 8;
            num2 ^= num;
            var num3 = num + num;
            num2 = ~num2;
            num2 = (num2 >> 7) & 1;
            num = num2 | num3;
        }
        return num;
    }

    private static void CalcPKeyFromNKey(int nkey, int dataoffset, out int pk1, out int pk2)
    {
        long num = (uint) (nkey * 7L);
        var bigInteger = new BigInteger(5440514381186227205L);
        num += dataoffset;
        var bigInteger2 = bigInteger * num;
        var num2 = (long) (bigInteger2 >> 70);
        var num3 = num2 >> 63;
        num2 += num3;
        num3 = num2 * 217;
        num -= num3;
        pk1 = (int) (num & 0xFFFFFFFFu);
        long num4 = (uint) (nkey * 11L);
        num4 += dataoffset;
        num2 = 0L;
        num2 &= 0x3FF;
        num4 += num2;
        num4 &= 0x3FF;
        var num5 = num4 - num2;
        pk2 = (int) (num5 & 0xFFFFFFFFu);
    }

    public ACE7XORKey(string fname)
    {
        NameKey = CalcNameKey(fname);
        Offset = 4;
        CalcPKeyFromNKey(NameKey, Offset, out pk1, out pk2);
    }
}