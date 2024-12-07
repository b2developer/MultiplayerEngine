using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class HuffmanCode
{
    public byte value;
    public BitStream codeStream;

    public HuffmanCode(byte _value, BitStream _codeStream)
    {
        value = _value;
        codeStream = _codeStream;
    }
}

//generates a huffman codes for bytes given their frequencies
public class HuffmanEncoder : MonoBehaviour
{
    public HuffmanCode[] hashTable;
    public HuffmanTree tree;

    public HuffmanEncoder()
    {
        hashTable = new HuffmanCode[256];
    }

    public void LoadFromFrequencyTable(string path)
    {
        FrequencyTable table = new FrequencyTable();
        table.ReadFromFile(path);

        tree = new HuffmanTree();
        tree.GenerateTree(table);
        tree.GenerateCodes();

        List<HuffmanNode> leafs = tree.GetLeafs();

        int count = leafs.Count;

        //generate hash table
        for (int i = 0; i < count; i++)
        {
            HuffmanNode leaf = leafs[i];
            hashTable[leaf.value] = new HuffmanCode(leaf.value, leaf.codeStream);
        }
    }

    public void WriteToStream(ref BitStream stream, byte uncompressed)
    {
        HuffmanCode code = hashTable[uncompressed];
        stream.WriteBytes(code.codeStream.buffer, code.codeStream.bitIndex);
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
        BitStream outStream = new BitStream(MaxDecompressionSize(stream.buffer.Length));

        bool force = true;
        HuffmanNode result = null;

        while (result != null || force)
        {
            force = false;
            result = tree.GetLeafFromCode(ref stream);

            if (result != null)
            {
                outStream.WriteBits(result.value, 8);
            }
        }

        int bitsWritten = outStream.bitIndex & 0x7;
        int bytesWritten = outStream.bitIndex >> 3;

        if (bitsWritten > 0)
        {
            bytesWritten++;
        }

        byte[] trimmedBuffer = new byte[bytesWritten];
        System.Buffer.BlockCopy(outStream.buffer, 0, trimmedBuffer, 0, bytesWritten);

        return trimmedBuffer;
    }

    public int MaxCompressionSize(int byteLength)
    {
        //256 bits per character (32 bytes)
        return byteLength * 32;
    }

    public int MaxDecompressionSize(int byteLength)
    {
        return byteLength * 8;
    }
}