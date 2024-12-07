using JetBrains.Annotations;
using System.Buffers.Binary;

public class Hash
{
    public static int ComputeHash(byte[] data, int length)
    {
        unchecked
        {
            const int p = 16777619;
            int hash = (int)2166136261;

            for (int i = 0; i < length; i++)
            {
                hash = (hash ^ data[i]) * p;
            }

            return hash;
        }
    }

    public static byte[] SignBuffer(byte[] buffer)
    {
        int length = buffer.Length;
        int withHashLength = length + 4;

        byte[] signedBuffer = new byte[withHashLength];
        System.Buffer.BlockCopy(buffer, 0, signedBuffer, 0, length);

        int hash = ComputeHash(buffer, length);

        if (Settings.PLATFORM_ENDIANNESS != Settings.STREAM_ENDIANESS)
        {
            hash = BinaryPrimitives.ReverseEndianness(hash);
        }

        BitStream stream = new BitStream(signedBuffer);
        stream.bitIndex = length * 8;

        int versionHash = hash ^ Settings.VERSION;

        stream.WriteInt(versionHash, 32);

        return signedBuffer;
    }

    public static bool VerifyBuffer(ref byte[] buffer, int length)
    {
        if (length <= 4)
        {
            return false;
        }

        int withoutHashLength = length - 4;

        BitStream stream = new BitStream(buffer);
        stream.bitIndex = withoutHashLength * 8;

        int givenHash = stream.ReadInt(32);

        int hash = ComputeHash(buffer, withoutHashLength);

        if (Settings.PLATFORM_ENDIANNESS != Settings.STREAM_ENDIANESS)
        {
            hash = BinaryPrimitives.ReverseEndianness(hash);
        }

        int versionHash = hash ^ Settings.VERSION;

        //trim signature out
        byte[] trimmedBuffer = new byte[Settings.BUFFER_SIZE];
        System.Buffer.BlockCopy(buffer, 0, trimmedBuffer, 0, withoutHashLength);

        buffer = trimmedBuffer;

        return versionHash == givenHash;
    }
}