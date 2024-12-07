using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;

public class BitStream
{
    public byte[] buffer;

    public int bitIndex = 0;

    public BitStream()
    {
        buffer = new byte[Settings.BUFFER_SIZE];
    }

    public BitStream(int bufferSize)
    {
        buffer = new byte[bufferSize];
    }

    public BitStream(byte[] _buffer)
    {
        buffer = _buffer;
    }

    public bool IsEnd()
    {
        return bitIndex >= buffer.Length * 8;
    }

    //writes a byte into the buffer bit by bit
    public void WriteBits(byte data, int bitCount)
    {
        int nextBitIndex = bitIndex + bitCount;

        int byteOffset = bitIndex >> 3; //remove last 3 bits (divide by 8 and round down)
        int bitOffset = bitIndex & 0x7; //get first 3 bits

        byte currentMask = (byte)~(0xff << bitOffset); //calculate which bits of current byte will be preserved

        //left half - mask the current byte
        //right half - shift the new data forward to 'slot' next to the original data
        buffer[byteOffset] = (byte)((buffer[byteOffset] & currentMask) | (data << bitOffset));

        //calculate how many bits were free
        int bitsFree = 8 - bitOffset;

        //check for overflow
        if (bitsFree < bitCount)
        {
            //this comes in handy when you're rewriting to the buffer
            byte nextMask = (byte)~currentMask;
            int nextByteOffset = byteOffset + 1;

            //shift unused new data forward, since it's already been partially written
            buffer[nextByteOffset] = (byte)((buffer[nextByteOffset] & nextMask) | (data >> bitsFree));
        }

        bitIndex = nextBitIndex;
    }

    //writes a byte into the buffer bit by bit, but without writing extra data
    public void WriteBitsSafe(byte data, int bitCount)
    {
        int nextBitIndex = bitIndex + bitCount;

        int byteOffset = bitIndex >> 3; //remove last 3 bits (divide by 8 and round down)
        int bitOffset = bitIndex & 0x7; //get first 3 bits

        byte currentMask = (byte)~(0xff << bitOffset); //calculate which bits of current byte will be preserved
        byte dataMask = (byte)~(0xff << bitCount); //calculate what bits need to be writen

        //left half - mask the current byte
        //right half - shift the new data forward to 'slot' next to the original data
        buffer[byteOffset] = (byte)((buffer[byteOffset] & currentMask) | ((data & dataMask) << bitOffset));

        //calculate how many bits were free
        int bitsFree = 8 - bitOffset;

        //check for overflow
        if (bitsFree < bitCount)
        {
            //this comes in handy when you're rewriting to the buffer
            byte nextMask = (byte)~currentMask;
            int nextByteOffset = byteOffset + 1;

            //shift unused new data forward, since it's already been partially written
            buffer[nextByteOffset] = (byte)((buffer[nextByteOffset] & nextMask) | (data >> bitsFree));
        }

        bitIndex = nextBitIndex;
    }

    //self contained min function
    public int Min(int a, int b)
    {
        if (a > b)
        {
            return b;
        }

        return a;
    }

    //writes a set amount of data into the buffer
    public unsafe void WriteBits(byte* data, int bitCount)
    {
        while (bitCount > 8)
        {
            WriteBits(*data, 8);
            data++;
            bitCount -= 8;
        }

        if (bitCount > 0)
        {
            WriteBits(*data, bitCount);
        }
    }

    //writes a set amount of data into the buffer
    public unsafe void WriteBitsSafe(byte* data, int bitCount)
    {
        while (bitCount > 8)
        {
            WriteBitsSafe(*data, 8);
            data++;
            bitCount -= 8;
        }

        if (bitCount > 0)
        {
            WriteBitsSafe(*data, bitCount);
        }
    }


    //writes a byte array into the buffer 
    public unsafe void WriteBytes(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            WriteBits(data[i], 8);
        }
    }

    //writes a byte array into the buffer with specified length
    public unsafe void WriteBytes(byte[] data, int length)
    {
        for (int i = 0; i < data.Length; i++)
        {
            int bitsToWrite = Math.Min(length, 8);
            WriteBits(data[i], bitsToWrite);

            length -= 8;

            if (length <= 0)
            {
                break;
            }
        }
    }

    //writes an int to the buffer bit by bit
    public unsafe void WriteInt(int data, int bitCount)
    {
        if (Settings.PLATFORM_ENDIANNESS != Settings.STREAM_ENDIANESS)
        {
            data = BinaryPrimitives.ReverseEndianness(data);
        }

        //get the start of the int, pretend it's a byte
        byte* currentByte = (byte*)&data;
        WriteBits(currentByte, bitCount);
    }

    //writes an int to the buffer bit by bit
    public unsafe void WriteUint(uint data, int bitCount)
    {
        if (Settings.PLATFORM_ENDIANNESS != Settings.STREAM_ENDIANESS)
        {
            data = BinaryPrimitives.ReverseEndianness(data);
        }

        //get the start of the int, pretend it's a byte
        byte* currentByte = (byte*)&data;
        WriteBits(currentByte, bitCount);
    }

    //writes an float to the buffer bit by bit
    public unsafe void WriteFloat(float data, int bitCount)
    {
        if (Settings.PLATFORM_ENDIANNESS != Settings.STREAM_ENDIANESS)
        {
            data = (float)BinaryPrimitives.ReverseEndianness((int)data);
        }

        //get the start of the float, pretend it's a byte
        byte* currentByte = (byte*)&data;
        WriteBits(currentByte, bitCount);
    }

    //writes a boolean to the buffer using only 1 bit
    public void WriteBool(bool data)
    {
        byte flag = data ? (byte)0x1 : (byte)0x0;
        WriteBits(flag, 1);
    }
    
    //basic bit reading function
    public byte[] ReadBits(int bitCount)
    {
        int byteCount = bitCount >> 3;
        int extraBits = bitCount & 0x7;
        int byteLength = byteCount;

        if (extraBits > 0)
        {
            byteLength++;
        }

        BitStream stream = new BitStream(byteLength);

        while (bitCount > 0)
        {
            int byteOffset = bitIndex >> 3; //remove last 3 bits (divide by 8 and round down)
            int bitOffset = bitIndex & 0x7; //get first 3 bits

            int bitsToWrite = Min(8 - bitOffset, bitCount);

            byte write = buffer[byteOffset];
            write = (byte)(write >> bitOffset);

            stream.WriteBitsSafe(write, bitsToWrite);

            bitCount -= bitsToWrite;
            bitIndex += bitsToWrite;
        }

        return stream.buffer;
    }

    //reads a set amount of data into a buffer
    public unsafe void ReadBits(byte* data, int bitCount)
    {
        byte[] inBuffer = ReadBits(bitCount);
        int index = 0;

        while (bitCount > 8)
        {
            *data = inBuffer[index];

            data++;
            index++;

            bitCount -= 8;
        }

        if (bitCount > 0)
        {
            byte dataMask = (byte)~(0xff << bitCount); //calculate what bits need to be writen
            *data = (byte)(inBuffer[index] & dataMask);
        }
    }

    //reads an int from the buffer bit by bit
    public unsafe int ReadInt(int bitCount)
    {
        int value = 0;
        byte* currentByte = (byte*)&value;

        ReadBits(currentByte, bitCount);

        if (Settings.PLATFORM_ENDIANNESS != Settings.STREAM_ENDIANESS)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    //reads an int from the buffer bit by bit
    public unsafe uint ReadUint(int bitCount)
    {
        uint value = 0;
        byte* currentByte = (byte*)&value;

        ReadBits(currentByte, bitCount);

        if (Settings.PLATFORM_ENDIANNESS != Settings.STREAM_ENDIANESS)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return value;
    }

    //reads a float from the buffer bit by bit
    public unsafe float ReadFloat(int bitCount)
    {
        float value = 0.0f;
        byte* currentByte = (byte*)&value;

        ReadBits(currentByte, bitCount);

        if (Settings.PLATFORM_ENDIANNESS != Settings.STREAM_ENDIANESS)
        {
            value = (float)BinaryPrimitives.ReverseEndianness((int)value);
        }

        return value;
    }

    //reads a boolean using only 1 bit
    public unsafe bool ReadBool()
    {
        bool value;
        byte[] bytes = ReadBits(1);

        value = (bytes[0] > 0) ? true : false;

        return value;
    }

    //byte swap for 2 byte storage
    public ushort ByteSwap2(ushort value)
    {
        return (ushort)((value >> 8) | (value << 8));
    }

    //byte swap for 4 byte storage
    public uint ByteSwap4(uint value)
    {
        return (value >> 24 & 0x000000ff) |
                (value >> 8 & 0x0000ff00) |
                (value << 8 & 0x00ff0000) |
                (value << 24 & 0xff000000);
    }

    //byte swap for 8 byte storage
    public ulong ByteSwap8(ulong value)
    {
        return (value >> 56 & 0x00000000000000ff) |
                (value >> 40 & 0x000000000000ff00) |
                (value >> 24 & 0x0000000000ff0000) |
                (value >> 8 & 0x00000000ff000000) |
                (value << 8 & 0x000000ff00000000) |
                (value << 24 & 0x0000ff0000000000) |
                (value << 40 & 0x00ff000000000000) |
                (value << 56 & 0xff00000000000000);

    }
}
