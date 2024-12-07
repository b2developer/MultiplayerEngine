using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySoundParams : RPCParams
{
    public int soundIndex = 0;

    public PlaySoundParams()
    {
        type = EFunction.PLAY_SOUND;
    }

    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);

        stream.WriteInt((int)soundIndex, 4);
    }

    public override void ReadFromStream(ref BitStream stream)
    {
        base.ReadFromStream(ref stream);

        soundIndex = stream.ReadInt(4);
    }

    public override int GetBitLength()
    {
        int total = base.GetBitLength();
        total += 4;

        return total;
    }
}
