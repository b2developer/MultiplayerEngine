using System.Collections;
using System.Collections.Generic;

public class Bucket
{
    public List<uint> tickets;

    public Bucket(int count)
    {
        tickets = new List<uint>();

        for (uint i = 0; i < count; i++)
        {
            tickets.Add(i);
        }
    }

    public bool IsAvailable()
    {
        return tickets.Count >= 1;
    }

    public void ReturnIndex(uint index)
    {
        tickets.Add(index);
    }

    public uint GetFreeIndex()
    {
        uint ticket = tickets[0];
        tickets.RemoveAt(0);

        return ticket;
    }
}
