using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColorParams : RPCParams
{
    public uint id = 0;

    public float r = 0.0f;
    public float g = 0.0f;
    public float b = 0.0f;

    public ChangeColorParams()
    {
        type = EFunction.CHANGE_COLOR;
    }

    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);

        stream.WriteUint(id, Settings.MAX_ENTITY_BITS);
        stream.WriteFloat(r, 32);
        stream.WriteFloat(g, 32);
        stream.WriteFloat(b, 32);
    }

    public override void ReadFromStream(ref BitStream stream)
    {
        base.ReadFromStream(ref stream);

        id = stream.ReadUint(Settings.MAX_ENTITY_BITS);
        r = stream.ReadFloat(32);
        g = stream.ReadFloat(32);
        b = stream.ReadFloat(32);
    }

    public override int GetBitLength()
    {
        int total = base.GetBitLength();
        total += Settings.MAX_ENTITY_BITS;
        total += 96;

        return total;
    }
}
