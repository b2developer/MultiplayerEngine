using System.Collections;
using System.Collections.Generic;

//using frequency analysis, 30% of all bytes are 0
//in practice, this simple encoder saves 20%
public class SimpleEntropyEncoder
{
    public void WriteToStream(ref BitStream stream, byte uncompressed)
    {
        if (uncompressed == 0x0)
        {
            stream.WriteBits(0x1, 1);
        }
        else
        {
            stream.WriteBits(0x0, 1);
            stream.WriteBits(uncompressed, 8);
        }
    }

    public void WriteCompressedBytes(ref BitStream stream, byte[] buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            WriteToStream(ref stream, buffer[i]);
        }
    }

    public byte[] ReadCompressedBytes(ref BitStream stream)
    {
        int maxBits = stream.buffer.Length * 8;
        int maxSize = MaxDecompressionSize(stream.buffer.Length);

        byte[] buffer = new byte[maxSize];

        int index = 0;

        while (!stream.IsEnd())
        {
            bool isZero = stream.ReadBool();

            if (isZero)
            {
                buffer[index] = 0x0;
            }
            else
            {
                //check we can read this many bits in
                if (stream.bitIndex <= maxBits - 8)
                {
                    buffer[index] = stream.ReadBits(8)[0];
                }
            }

            index++;
        }

        byte[] trimmedBuffer = new byte[index];
        System.Buffer.BlockCopy(buffer, 0, trimmedBuffer, 0, index);

        return trimmedBuffer;
    }

    public int MaxCompressionSize(int byteLength)
    {
        return (int)System.Math.Ceiling(byteLength * (9.0f / 8.0f));
    }

    public int MaxDecompressionSize(int byteLength)
    {
        return byteLength * 8;
    }
}
