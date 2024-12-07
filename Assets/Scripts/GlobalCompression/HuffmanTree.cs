using System;
using System.Collections;
using System.Collections.Generic;

//tree structure for huffman nodes
public class HuffmanTree
{
    public HuffmanNode root = null;

    public HuffmanTree()
    {

    }

    //insert into tree structure
    public void InsertIntoTree(HuffmanNode node)
    {
        if (root == null)
        {
            root = node;
        }
        else
        {
            root.Insert(node);
        }
    }

    //insert into sorted array
    public void InsertIntoArray(ref List<HuffmanNode> list, HuffmanNode node)
    {
        int count = list.Count;

        for (int i = 0; i < count; i++)
        {
            if (node.frequency < list[i].frequency)
            {
                list.Insert(0, node);
                return;
            }
        }

        //no smaller entry found, add to back
        list.Add(node);
    }

    public void GenerateTree(FrequencyTable table)
    {
        List<HuffmanNode> nodes = new List<HuffmanNode>();

        for (int i = 0; i < 256; i++)
        {
            HuffmanNode node = new HuffmanNode();
            node.value = table.values[i];
            node.frequency = table.occurences[i];

            nodes.Add(node);
        }

        //sort the list 
        nodes.Sort();

        //>=2 ensures that we stop after there are no more pairs
        while (nodes.Count >= 2)
        {
            //generate intermediate node by combining 2 smallest nodes
            HuffmanNode z = new HuffmanNode();

            z.isIntermediate = true;

            z.leftChild = nodes[0];
            z.rightChild = nodes[1];
            
            z.frequency = z.leftChild.frequency + z.rightChild.frequency;

            //remove the first 2 entries
            nodes.RemoveRange(0, 2);

            //add the intermediate node into the nodes list
            InsertIntoArray(ref nodes, z);
        }

        //last element is the root node
        root = nodes[0];
    }


    //start the generative code process on the root node
    public void GenerateCodes()
    {
        BitStream startingStream = new BitStream(32);
        root.GenerateCodes(startingStream);
    }

    public List<HuffmanNode> GetLeafs()
    {
        List<HuffmanNode> list = new List<HuffmanNode>();
        root.GetLeafs(ref list);
        return list;
    }

    public HuffmanNode GetLeafFromCode(ref BitStream stream)
    {
        return root.GetLeafFromCode(ref stream);
    }
}

public class HuffmanNode : IComparable
{
    //value of code
    public byte value = 0x0;

    //indicates if this is a combined node
    public bool isIntermediate = false;

    //frequency of code (or codes if intermediate)
    public uint frequency = 0;

    //tree structure
    public HuffmanNode leftChild = null;
    public HuffmanNode rightChild = null;

    //code variables
    public BitStream codeStream;

    public HuffmanNode()
    {
        codeStream = new BitStream(32);
    }

    //built-in sorter can use this to compare huffman nodes
    public int CompareTo(object obj)
    {
        if (obj == null)
        {
            return 1;
        }

        HuffmanNode otherNode = obj as HuffmanNode;
    
        if (otherNode != null)
        {
            return frequency.CompareTo(otherNode.frequency);
        }
        else
        {
            throw new ArgumentException("Object is not a HuffmanNode");
        }
    }

    public void Insert(HuffmanNode node)
    {
        if (node.frequency < frequency)
        {
            if (leftChild == null)
            {
                leftChild = node;
            }
            else
            {
                leftChild.Insert(node);
            }
        }
        else //node.frequency >= frequency
        {
            if (rightChild == null)
            {
                rightChild = node;
            }
            else
            {
                rightChild.Insert(node);
            }
        }
    }

    //calculates the binary values of each leaf in the tree
    public void GenerateCodes(BitStream _stream)
    {
        if (leftChild != null)
        {
            byte[] newBuffer = new byte[32];
            System.Buffer.BlockCopy(_stream.buffer, 0, newBuffer, 0, 32);

            BitStream newStream = new BitStream(newBuffer);
            newStream.bitIndex = _stream.bitIndex;
            newStream.WriteBits(0x0, 1);

            leftChild.GenerateCodes(newStream);
        }

        if (rightChild != null)
        {
            byte[] newBuffer = new byte[32];
            System.Buffer.BlockCopy(_stream.buffer, 0, newBuffer, 0, 32);

            BitStream newStream = new BitStream(newBuffer);
            newStream.bitIndex = _stream.bitIndex;
            newStream.WriteBits(0x1, 1);

            rightChild.GenerateCodes(newStream);
        }

        //we have reached the end
        if (leftChild == null && rightChild == null)
        {
            byte[] newBuffer = new byte[32];
            System.Buffer.BlockCopy(_stream.buffer, 0, newBuffer, 0, 32);

            codeStream.buffer = newBuffer;
            codeStream.bitIndex = _stream.bitIndex;
        }
    }

    public void GetLeafs(ref List<HuffmanNode> list)
    {
        if (leftChild != null)
        {
            leftChild.GetLeafs(ref list);
        }

        if (rightChild != null)
        {
            rightChild.GetLeafs(ref list);
        }

        if (leftChild == null && rightChild == null)
        {
            list.Add(this);
        }
    }

    public HuffmanNode GetLeafFromCode(ref BitStream stream)
    {
        if (leftChild == null && rightChild == null)
        {
            return this;
        }

        //end of stream detected
        if (stream.bitIndex > stream.buffer.Length * 8)
        {
            return null;
        }

        bool child = stream.ReadBool();

        if (child)
        {
            return rightChild.GetLeafFromCode(ref stream);
        }
        else
        {
            return leftChild.GetLeafFromCode(ref stream);
        }
    }
}