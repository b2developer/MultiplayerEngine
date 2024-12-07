using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class RPCManager : MonoBehaviour
{
    public EntityManager entityManager;

    public List<RPCParams> rpcsToSend;

    public AudioSource source;
    public AudioClip[] clips;

    void Start()
    {
        rpcsToSend = new List<RPCParams>();
    }

    public void ExecuteFunction(ref BitStream stream)
    {
        EFunction type = (EFunction)stream.ReadInt(Settings.MAX_FUNCTION_TYPE_BITS);
        stream.bitIndex -= Settings.MAX_FUNCTION_TYPE_BITS;

        if (type == EFunction.PLAY_SOUND)
        {
            PlaySoundParams psp = new PlaySoundParams();
            psp.ReadFromStream(ref stream);

            PlaySoundClient(psp.soundIndex);
        }
        else if (type == EFunction.CHANGE_COLOR)
        {
            ChangeColorParams ccp = new ChangeColorParams();
            ccp.ReadFromStream(ref stream);

            ChangeColorClient(ccp.id, ccp.r, ccp.g, ccp.b);
        }
    }

    //writes all replication data, without any sort of overflow protection
    public void WriteReplicationData(ref BitStream stream)
    {
        int paramsCount = rpcsToSend.Count;

        for (int i = 0; i < paramsCount; i++)
        {
            stream.WriteBits((int)EReplication.RPC, 3);

            RPCParams rpc = rpcsToSend[i];
            rpc.WriteToStream(ref stream);
        }
    }

    public bool ReadyToWriteReliableData()
    {
        return rpcsToSend.Count > 0;
    }

    //writes all replication data for reliable sources, with multipass for loops
    public void WriteReplicationDataReliableIndexed(ref BitStream stream, ref WriteReplicationIndexer indexer, int totalBitIndex, uint offset, uint written)
    {
        int maxBits = Settings.BUFFER_LIMIT * 8;

        int paramsCount = rpcsToSend.Count;

        uint originalWriteCount = indexer.writeCount;

        for (uint i = indexer.writeCount - offset; i < paramsCount; i++)
        {
            RPCParams rpc = rpcsToSend[(int)i];

            int size = rpc.GetBitLength() + 3;

            if (stream.bitIndex + size > maxBits)
            {
                int originalIndex = stream.bitIndex;

                stream.bitIndex = totalBitIndex;
                stream.WriteUint(indexer.writeCount - originalWriteCount + written, 32);

                stream.bitIndex = originalIndex;
                return;
            }

            stream.WriteBits((int)EReplication.RPC, 3);
            rpc.WriteToStream(ref stream);

            indexer.totalIndex++;
            indexer.writeCount++;
        }

        indexer.isDone = true;
    }

    public void FinishReplicationData()
    {
        rpcsToSend.Clear();
    }

    //implementations of rpcs / rmis
    //----------

    public void PlaySoundClient(int soundIndex)
    {
        source.clip = clips[soundIndex];
        source.Play();
    }

    public void PlaySoundServer(int soundIndex)
    {
        source.clip = clips[soundIndex];
        source.Play();

        PlaySoundParams psp = new PlaySoundParams();
        psp.soundIndex = soundIndex;
        rpcsToSend.Add(psp);
    }

    public void ChangeColorClient(uint id, float r, float g, float b)
    {
        Entity entity = entityManager.GetEntityFromId(id);

        if (entity == null)
        {
            return;
        }

        ChameleonEntity chameleon = entity as ChameleonEntity;
        chameleon.ChangeColor(r, g, b);
    }

    public void ChangeColorServer(uint id, float r, float g, float b)
    {
        Entity entity = entityManager.GetEntityFromId(id);

        if (entity == null)
        {
            return;
        }

        ChameleonEntity chameleon = entity as ChameleonEntity;
        chameleon.ChangeColor(r, g, b);

        ChangeColorParams ccp = new ChangeColorParams();
        ccp.id = id;
        ccp.r = r;
        ccp.g = g;
        ccp.b = b;
        rpcsToSend.Add(ccp);
    }
}
