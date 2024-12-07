using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EFunction
{
    PLAY_SOUND = 0,
    CHANGE_COLOR = 1,
    MAX = 2,
}

public class RPCParams
{
    public EFunction type;

    public virtual void WriteToStream(ref BitStream stream)
    {
        stream.WriteInt((int)type, Settings.MAX_FUNCTION_TYPE_BITS);
    }

    public virtual void ReadFromStream(ref BitStream stream)
    {
        type = (EFunction)stream.ReadInt(Settings.MAX_FUNCTION_TYPE_BITS);
    }

    public virtual int GetBitLength()
    {
        return Settings.MAX_FUNCTION_TYPE_BITS;
    }
}
