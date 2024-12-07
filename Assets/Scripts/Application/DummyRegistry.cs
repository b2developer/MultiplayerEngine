using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//small class used to eat stream data and advance it properly
public class DummyRegistry : MonoBehaviour
{
    public bool initialised = false;

    public ObjectRegistry objectRegistry;

    public Transform dummyFolder;
    public List<Entity> dummyPrefabs;

    public void Initialise()
    {
        //setup fake stream to trick object registry
        byte[] emptyBuffer = new byte[Settings.BUFFER_SIZE];
        BitStream emptyStream = new BitStream(emptyBuffer);

        emptyStream.WriteInt(0, Settings.MAX_ENTITY_BITS);

        EObject[] types = (EObject[])System.Enum.GetValues(typeof(EObject));

        foreach (EObject type in types)
        {
            if (type == EObject.MAX)
            {
                continue;   
            }

            emptyStream.bitIndex = Settings.MAX_ENTITY_BITS;
            emptyStream.WriteInt((int)type, Settings.MAX_TYPE_BITS);
            emptyStream.bitIndex = 0;

            Entity entity = objectRegistry.CreateObjectClient(ref emptyStream);
            entity.Initialise();

            entity.transform.SetParent(dummyFolder);
            dummyPrefabs.Add(entity);
        }

        initialised = true;
    }

    public void AdvanceStream(ref BitStream stream)
    {
        if (!initialised)
        {
            Initialise();
        }

        stream.bitIndex += Settings.MAX_ENTITY_BITS;
        EObject type = (EObject)stream.ReadInt(Settings.MAX_TYPE_BITS);
        stream.bitIndex -= Settings.MAX_ENTITY_BITS + Settings.MAX_TYPE_BITS;

        dummyPrefabs[(int)type].ReadFromStream(ref stream);
    }

    public void AdvanceStreamPartial(ref BitStream stream)
    {
        if (!initialised)
        {
            Initialise();
        }

        stream.bitIndex += Settings.MAX_ENTITY_BITS;
        EObject type = (EObject)stream.ReadInt(Settings.MAX_TYPE_BITS);
        stream.bitIndex -= Settings.MAX_ENTITY_BITS + Settings.MAX_TYPE_BITS;

        dummyPrefabs[(int)type].ReadFromStreamPartial(ref stream);
    }
}
