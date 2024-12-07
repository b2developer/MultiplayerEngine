using UnityEngine;

//elias gamma codes variable length positive intergers
public class EliasEncoding
{
    public static void WriteToStream(ref BitStream stream, uint value)
    {
        //edge case for 1
        if (value == 1)
        {
            stream.WriteInt(0x1, 1);
            return;
        }

        int zeroes = MathExtension.RequiredBits(value);

        stream.WriteBits(0x0, zeroes);
        stream.WriteBits(0x1, 1);

        stream.WriteUint(value, zeroes);
    }

    public static uint ReadFromStream(ref BitStream stream)
    {
        int zeroCount = 0;
        bool state = stream.ReadBool();

        while (state)
        {
            zeroCount++;
            stream.ReadBool();
        }

        if (zeroCount == 0)
        {
            return 1;
        }

        //1 was read in
        uint pow2 = (uint)(1 << zeroCount);

        uint add = stream.ReadUint(zeroCount);
        return pow2 + add;
    }

    //simple wrapper for smart writer for ints
    public static int SmartWriteToStream(ref BitStream stream, int value, int maxLength)
    {
        return SmartWriteToStream(ref stream, (uint)value, maxLength);
    }

    public static int SmartWriteToStream(ref BitStream stream, uint value, int maxLength)
    {
        int zeroes = MathExtension.RequiredBits(value);
        int eliasLength = zeroes * 2 + 1;

        //check if elias can save some space
        if (eliasLength > maxLength)
        {
            stream.WriteBits(0x1, 1);
            stream.WriteUint(value, maxLength);

            return maxLength;
        }
        else
        {
            stream.WriteBits(0x0, 1);
            WriteToStream(ref stream, value);

            return eliasLength;
        }
    }

    public static uint SmartReadFromStream(ref BitStream stream, int maxLength)
    {
        bool isElias = stream.ReadBool();

        if (isElias)
        {
            return ReadFromStream(ref stream);
        }
        else
        {
            return stream.ReadUint(maxLength);
        }
    }
}
