using System.Collections;
using System.Collections.Generic;

public class RunLengthData
{
    public List<bool> states;
    public List<int> lengths;

    public int maxLength = 64;
    public int bitLength = 0;

    public RunLengthData()
    {
        states = new List<bool>();
        lengths = new List<int>();

        bitLength = MathExtension.RequiredBits(maxLength);
    }

    public void Update(bool state)
    {
        int count = states.Count;
        int last = count - 1;

        if (count <= 0)
        {
            states.Add(state);
            lengths.Add(1);
        }
        else if (state == states[last])
        {
            if (lengths[last] >= maxLength - 1)
            {
                //max run length reached
                states.Add(state);
                lengths.Add(1);
            }
            else
            {
                //append to latest run
                lengths[last]++;
            }
        }
        else
        {

            //new state detected
            states.Add(state);
            lengths.Add(1);
        }
    }

    public void AddState(bool state, int length)
    {
        states.Add(state);
        lengths.Add(length);
    }
}

//encodes RLE but uses elias encoding for the run lengths
public class EliasRunLengthEncoder
{
    public void EncodeLength(ref BitStream stream, int length)
    {
        //edge case for 1
        if (length == 1)
        {
            stream.WriteInt(0x1, 1);
            return;
        }

        int zeroes = MathExtension.RequiredBits(length);

        stream.WriteBits(0x0, zeroes);
        stream.WriteBits(0x1, 1);

        stream.WriteInt(length, zeroes);
    }

    public int DecodeLength(ref BitStream stream)
    {
        int zeroCount = 0;

        if (stream.IsEnd())
        {
            return -1;
        }

        bool state = stream.ReadBool();

        while (state)
        {
            if (stream.IsEnd())
            {
                return -1;
            }

            zeroCount++;
            stream.ReadBool();
        }

        if (zeroCount == 0)
        {
            return 1;
        }

        //1 was read in
        int pow2 = 1 << zeroCount;

        int maxBits = stream.buffer.Length * 8;

        if (stream.bitIndex <= maxBits - zeroCount)
        {
            return -1;
        }

        int add = stream.ReadInt(zeroCount);
        return pow2 + add;
    }

    public void WriteCompressedBytes(ref BitStream stream, byte[] buffer)
    {
        RunLengthData data = new RunLengthData();

        //first pass to detect runs
        BitStream inStream = new BitStream(buffer);

        while (!inStream.IsEnd())
        {
            bool state = inStream.ReadBool();
            data.Update(state);
        }

        //second pass to write runs
        int runCount = data.lengths.Count;

        for (int i = 0; i < runCount; i++)
        {
            bool state = data.states[i];
            int length = data.lengths[i];

            stream.WriteBool(state);
            EncodeLength(ref stream, length);
        }
    }

    public byte[] ReadCompressedBytes(ref BitStream stream)
    {
        RunLengthData data = new RunLengthData();

        int maxSize = MaxDecompressionSize(stream.buffer.Length);

        while (!stream.IsEnd())
        {
            bool state = stream.ReadBool();
            int length = DecodeLength(ref stream);

            if (length < 0)
            {
                break;
            }

            data.AddState(state, length);
        }

        BitStream outStream = new BitStream(maxSize);

        //second pass to write decompressed runs
        int runCount = data.lengths.Count;

        for (int i = 0; i < runCount; i++)
        {
            bool state = data.states[i];
            int length = data.lengths[i];

            uint mask = (uint)(state ? 0xff : 0x0);
            outStream.WriteUint(mask, length);
        }

        int bitsWritten = stream.bitIndex & 0x7;
        int bytesWritten = stream.bitIndex >> 3;

        if (bitsWritten > 0)
        {
            bytesWritten++;
        }

        byte[] trimmedBuffer = new byte[bitsWritten];
        System.Buffer.BlockCopy(outStream.buffer, 0, trimmedBuffer, 0, bytesWritten);

        return trimmedBuffer;
    }

    public int MaxCompressionSize(int byteLength)
    {
        RunLengthData data = new RunLengthData();

        int totalLength = data.bitLength + 1;
        return byteLength * totalLength;
    }

    public int MaxDecompressionSize(int byteLength)
    {
        RunLengthData data = new RunLengthData();

        //2 ^ runBitLength - 1 - (runBitLength + 1)
        int pow2 = ((1 << (data.bitLength - 1)) - 1);
        int totalLength = data.bitLength + 1;

        return byteLength * (pow2 - totalLength);
    }
}
