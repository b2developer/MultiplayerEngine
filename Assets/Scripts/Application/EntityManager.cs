using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using Unity.VisualScripting;
using UnityEngine;

public enum EReplication
{
    CREATE = 0,
    UPDATE = 1,
    DESTROY = 2,
    UPDATE_PARTIAL = 3,
    RPC = 4,
}

//used to allow for multi pass for-loops when writing packets
public class WriteReplicationIndexer
{
    public uint destroyIndex = 0;
    public uint totalIndex = 0;

    public uint createCount = 0;
    public uint writeCount = 0;

    public bool isDone = false;
}

public class TransformCache
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public TransformCache(Vector3 _position, Quaternion _rotation, Vector3 _scale)
    {
        position = _position;
        rotation = _rotation;
        scale = _scale;
    }

    public Vector3 TransformPoint(Vector3 point)
    {
        point = new Vector3(point.x * scale.x, point.y * scale.y, point.z * scale.z);
        point = rotation * point;
        point += position;

        return point;
    }

    public Vector3 TransformDirection(Vector3 direction)
    {
        direction = rotation * direction;

        return direction;
    }
}

public class BitCache
{
    public BitStream stream;
}

public class EntityManager : MonoBehaviour
{
    public static EntityManager instance = null;

    public int proxyId = -1;

    public ObjectRegistry objectRegistry;
    public RPCManager rpcManager;
    public DummyRegistry dummyRegistry;
    public UpdateManager updateManager;

    public CullingStack referenceCullingStack;

    public Bucket networkIds;
    public List<int> ticks;

    public List<Entity> entities;
    public LookupTable<Entity> lookup;
    public LookupTable<TransformCache> transformCacheLookup;
    public LookupTable<BitCache> bitCacheLookup;

    public List<uint> toDestroy;
    public List<uint> unknownToDestroy;

    public void Initialise()
    {
        if (instance == null)
        {
            instance = this;
        }

        referenceCullingStack = new CullingStack();

        networkIds = new Bucket(Settings.MAX_ENTITY_INDEX);
        ticks = new List<int>();

        entities = new List<Entity>();
        
        lookup = new LookupTable<Entity>(Settings.MAX_ENTITY_INDEX);
        transformCacheLookup = new LookupTable<TransformCache>(Settings.MAX_ENTITY_INDEX);
        bitCacheLookup = new LookupTable<BitCache>(Settings.MAX_ENTITY_INDEX);

        toDestroy = new List<uint>();
        unknownToDestroy = new List<uint>();

        GenerateFullTicks();

        //generate reference culling stack
        DistanceCulling distanceCulling = new DistanceCulling();
        distanceCulling.mode = ECullingMode.REQUIREMENT;
        distanceCulling.CalculateSquareDistance();
        distanceCulling.getTransformCacheCallback = GetTransformCacheData;
        referenceCullingStack.stack.Add(distanceCulling);

        FrustumCulling frustumCulling = new FrustumCulling();
        distanceCulling.mode = ECullingMode.OPTIONAL;
        frustumCulling.CalculateHorizontalFieldOfView();
        frustumCulling.getTransformCacheCallback = GetTransformCacheData;
        referenceCullingStack.stack.Add(frustumCulling);
    }

    public void Dump()
    {
        unknownToDestroy.Clear();

        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            Entity item = entities[i];
            lookup.Remove((int)item.id);
            transformCacheLookup.Remove((int)item.id);
            bitCacheLookup.Remove((int)item.id);

            updateManager.entityFunction -= item.Tick;

            if (item.type == EObject.PLAYER)
            {
                objectRegistry.destroyPlayerCallback(item);
            }
            else if (item.type == EObject.SHIP)
            {
                objectRegistry.destroyShipCallback(item);
            }
            else if (item.type == EObject.BIG_SHIP)
            {
                objectRegistry.destroyShipCallback(item);
            }
            else if (item.type == EObject.SMALL_SHIP)
            {
                objectRegistry.destroyShipCallback(item);
            }

            Destroy(item.gameObject);
        }

        entities.Clear();

        proxyId = -1;
    }

    public void DestroyClient(uint id)
    {
        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            Entity item = entities[i];

            if (item.id == id)
            {
                lookup.Remove((int)item.id);
                transformCacheLookup.Remove((int)item.id);

                updateManager.entityFunction -= item.Tick;

                if (item.type == EObject.PLAYER)
                {
                    objectRegistry.destroyPlayerCallback(item);
                }
                else if (item.type == EObject.SHIP)
                {
                    objectRegistry.destroyShipCallback(item);
                }
                else if (item.type == EObject.BIG_SHIP)
                {
                    objectRegistry.destroyShipCallback(item);
                }
                else if (item.type == EObject.SMALL_SHIP)
                {
                    objectRegistry.destroyShipCallback(item);
                }

                Destroy(item.gameObject);
                entities.RemoveAt(i);

                return;
            }
        }

        //item was not found, this is an out of order destroy command
        unknownToDestroy.Add(id);
    }

    public void DestroyServer(uint id)
    {
        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            Entity item = entities[i];

            if (item.id == id)
            {
                lookup.Remove((int)item.id);
                transformCacheLookup.Remove((int)item.id);
                bitCacheLookup.Remove((int)item.id);

                updateManager.entityFunction -= item.Tick;

                if (item.type == EObject.PLAYER)
                {
                    objectRegistry.destroyPlayerCallback(item);
                }
                else if (item.type == EObject.SHIP)
                {
                    objectRegistry.destroyShipCallback(item);
                }
                else if (item.type == EObject.BIG_SHIP)
                {
                    objectRegistry.destroyShipCallback(item);
                }
                else if (item.type == EObject.SMALL_SHIP)
                {
                    objectRegistry.destroyShipCallback(item);
                }

                Destroy(item.gameObject);

                entities.RemoveAt(i);

                networkIds.ReturnIndex(item.id);
                toDestroy.Add(item.id);

                break;
            }
        }
    }

    public Entity GetEntityFromId(uint id)
    {
        return lookup.Grab((int)id);
    }

    public void CheckForTimeouts()
    {
        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            Entity item = entities[i];

            if (!item.accept)
            {
                continue;
            }

            bool timeout = item.TickTimeout();

            if (timeout)
            {
                updateManager.entityFunction -= item.Tick;

                if (item.type == EObject.PLAYER)
                {
                    objectRegistry.destroyPlayerCallback(item);
                }

                Destroy(item.gameObject);
                entities.RemoveAt(i);

                break;
            }
        }
    }

    public void GenerateFullTicks()
    {
        for (int i = 0; i < Settings.PARTIAL_TO_FULL_RATIO; i++)
        {
            ticks.Add(i);
        }
    }

    public void AssignFullTick(Entity entity)
    {
        int randomIndex = Random.Range(0, ticks.Count);

        int randomTick = ticks[randomIndex];
        ticks.RemoveAt(randomIndex);

        entity.tick = randomTick;

        if (ticks.Count == 0)
        {
            GenerateFullTicks();
        }
    }

    public void SetPriorities(PlayerEntity player)
    {
        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            Entity item = entities[i];
            item.SetPriority(player);
        }

        entities.Sort();
    }

    public void CacheTransformData()
    {
        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            Entity item = entities[i];

            TransformEntity transformItem = item as TransformEntity;

            if (transformItem == null)
            {
                continue;
            }

            TransformCache transformCache = new TransformCache(transformItem.transform.position, transformItem.transform.rotation, transformItem.transform.localScale);
            transformCacheLookup.Place(transformCache, (int)item.id);
        }
    }

    public LookupTable<TransformCache> GetTransformCacheData()
    {
        return transformCacheLookup;
    }

    //writes all replication data, without any sort of overflow protection
    public void WriteReplicationDataNaive(ref BitStream stream)
    {
        uint destroyCount = (uint)toDestroy.Count;

        stream.WriteUint(destroyCount, Settings.MAX_ENTITY_BITS);

        //write all recently destroyed objects
        for (int i = 0; i < destroyCount; i++)
        {
            stream.WriteBits((int)EReplication.DESTROY, 3);
            stream.WriteUint(toDestroy[i], Settings.MAX_ENTITY_BITS);
        }

        int totalCount = entities.Count + rpcManager.rpcsToSend.Count;
        int entityCount = entities.Count;

        stream.WriteInt(totalCount, 32);

        for (int i = 0; i < entityCount; i++)
        {
            Entity item = entities[i];

            if (item.isNew)
            {
                stream.WriteBits((int)EReplication.CREATE, 3);
                item.WriteToStream(ref stream);
            }
            else
            {
                stream.WriteBits((int)EReplication.UPDATE, 3);
                item.WriteToStream(ref stream);
            }
        }

        rpcManager.WriteReplicationData(ref stream);
    }

    //writes all replication data for unreliable sources, utilising dirty flags, with multipass for loops
    public WriteReplicationIndexer WriteReplicationDataPartialOnlyIndexed(ref BitStream stream, ref CullingStack cullingStack, WriteReplicationIndexer previousIndexer = null)
    {
        int maxBits = Settings.BUFFER_LIMIT * 8;

        WriteReplicationIndexer indexer = previousIndexer;

        if (indexer == null)
        {
            indexer = new WriteReplicationIndexer();
        }

        //write 0 destroys on unreliable data
        stream.WriteInt(0, Settings.MAX_ENTITY_BITS);

        //int totalCount = entities.Count + rpcManager.rpcsToSend.Count;
        int entityCount = entities.Count;

        int totalBitIndex = stream.bitIndex;
        stream.WriteInt(0, 32);

        uint originalWriteCount = indexer.writeCount;

        for (uint i = indexer.totalIndex; i < entityCount; i++)
        {
            Entity item = entities[(int)i];

            bool cullingTest = cullingStack.ApplyCulling(item);

            //check culling filter
            if (!cullingTest)
            {
                continue;
            }

            int size = item.GetBitLengthPartial() + 3;

            //check for overflow
            if (stream.bitIndex + size > maxBits)
            {
                //limit reached, rewrite total count
                int originalIndex = stream.bitIndex;

                stream.bitIndex = totalBitIndex;
                stream.WriteUint(indexer.writeCount - originalWriteCount, 32);

                stream.bitIndex = originalIndex;
                return indexer;
            }

            //check if item has partial data to send
            if (!item.isNew && item.dirtyFlag > 0)
            {
                stream.WriteBits((int)EReplication.UPDATE_PARTIAL, 3);
                item.WriteToStreamPartial(ref stream);
                indexer.writeCount++;
            }

            indexer.totalIndex++;
        }

        uint written = indexer.writeCount - originalWriteCount;

        //write final amount into buffer if all was successful
        int finalOriginalIndex = stream.bitIndex;

        stream.bitIndex = totalBitIndex;
        stream.WriteUint(written, 32);

        stream.bitIndex = finalOriginalIndex;

        indexer.isDone = true;

        return indexer;
    }

    public void FinishWritingReplicationData()
    {
        toDestroy.Clear();

        int entityCount = entities.Count;

        for (int i = 0; i < entityCount; i++)
        {
            Entity item = entities[i];

            if (item.isNew)
            {
                item.isNew = false;
            }
        }

        rpcManager.FinishReplicationData();
    }

    public void ReadReplicationData(ref BitStream stream)
    {
        uint destroyCount = stream.ReadUint(Settings.MAX_ENTITY_BITS);

        for (uint i = 0; i < destroyCount; i++)
        {
            stream.bitIndex += 3;
            uint idToDestroy = stream.ReadUint(Settings.MAX_ENTITY_BITS);

            DestroyClient(idToDestroy);
        }

        int count = stream.ReadInt(32);

        int entityCount = entities.Count;

        for (int i = 0; i < count; i++)
        {
            byte commandByte = stream.ReadBits(3)[0];
            EReplication command = (EReplication)commandByte;

            if (command == EReplication.CREATE)
            {
                //peek the id
                uint id = stream.ReadUint(Settings.MAX_ENTITY_BITS);
                stream.bitIndex -= Settings.MAX_ENTITY_BITS;

                bool doesntExist = true;
                bool wasDestroyed = unknownToDestroy.Contains(id);

                if (wasDestroyed)
                {
                    unknownToDestroy.Remove(id);
                }
                else
                {
                    Entity item = lookup.Grab((int)id);

                    if (item != null)
                    {
                        doesntExist = false;
                    }
                }

                if (doesntExist && !wasDestroyed)
                {
                    objectRegistry.RegisterObjectClient(ref stream);

                    //set active flag to false by default
                    Entity item = entities[^1];
                    item.SetActive(false);
                    item.idleTime = Settings.ENTITY_TIMEOUT_TIME;
                }
                else
                {
                    dummyRegistry.AdvanceStream(ref stream);
                }

                //side node: entityCount could be ++ed here, but there is no point: we just got the create data
            }
            else if (command == EReplication.UPDATE)
            {
                //peek the id
                int id = stream.ReadInt(Settings.MAX_ENTITY_BITS);
                stream.bitIndex -= Settings.MAX_ENTITY_BITS;

                bool skip = true;
                
                Entity item = lookup.Grab((int)id);

                if (item != null)
                {
                    item.ReadFromStream(ref stream);
                    skip = false;
                }

                //if the entity isn't found (we could replicate it, but spawning is reliable_unordered)
                if (skip)
                {
                    //this is mainly done to advance the bitstream bit index
                    dummyRegistry.AdvanceStream(ref stream);
                }
            }
            else if (command == EReplication.UPDATE_PARTIAL)
            {
                //peek the id
                int id = stream.ReadInt(Settings.MAX_ENTITY_BITS);
                stream.bitIndex -= Settings.MAX_ENTITY_BITS;

                bool skip = true;

                Entity item = lookup.Grab((int)id);

                if (item != null)
                {
                    item.ReadFromStreamPartial(ref stream);
                    skip = false;
                }

                //if the entity isn't found
                if (skip)
                {
                    //this is mainly done to advance the bitstream bit index
                    dummyRegistry.AdvanceStreamPartial(ref stream);
                }
            }
            else if (command == EReplication.RPC)
            {
                rpcManager.ExecuteFunction(ref stream);
            }
        }
    }

    //writes all replication data for unreliable sources, with multipass for loops
    public WriteReplicationIndexer WriteReplicationDataIndexed(ref BitStream stream, ref CullingStack cullingStack, WriteReplicationIndexer previousIndexer = null, int tick = 0, int limit = 0)
    {
        int maxBits = Settings.BUFFER_LIMIT * 8;

        WriteReplicationIndexer indexer = previousIndexer;

        if (indexer == null)
        {
            indexer = new WriteReplicationIndexer();
        }

        //write 0 destroys on unreliable data
        stream.WriteInt(0, Settings.MAX_ENTITY_BITS);

        //int totalCount = entities.Count + rpcManager.rpcsToSend.Count;
        int entityCount = entities.Count;

        //set hard entity limit from priority (if there is one)
        int entityLimit = entityCount + 1;

        if (limit > 0)
        {
            entityLimit = limit;
        }

        int totalBitIndex = stream.bitIndex;
        stream.WriteInt(0, 32);

        uint originalWriteCount = indexer.writeCount;

        for (uint i = indexer.totalIndex; i < entityCount; i++)
        {
            Entity item = entities[(int)i];

            bool cullingTest = cullingStack.ApplyCulling(item);

            //check culling filter
            if (!cullingTest)
            {
                continue;
            }

            int size = item.GetBitLength() + 3;

            //check for overflow
            if (stream.bitIndex + size > maxBits)
            {
                //limit reached, rewrite total count
                int originalIndex = stream.bitIndex;

                stream.bitIndex = totalBitIndex;
                stream.WriteUint(indexer.writeCount - originalWriteCount, 32);

                stream.bitIndex = originalIndex;
                return indexer;
            }

            //check whether a full update is due
            if (item.tick != tick)
            {
                //check if item has partial data to send
                if (!item.isNew && item.dirtyFlag > 0)
                {
                    stream.WriteBits((int)EReplication.UPDATE_PARTIAL, 3);
                    item.WriteToStreamPartial(ref stream);
                    indexer.writeCount++;
                }
            }
            else
            {
                if (!item.isNew)
                {
                    stream.WriteBits((int)EReplication.UPDATE, 3);
                    item.WriteToStream(ref stream);
                    indexer.writeCount++;
                }
            }

            //limit reached
            if (indexer.writeCount >= entityLimit)
            {
                uint written = indexer.writeCount - originalWriteCount;

                //write final amount into buffer if all was successful
                int finalOriginalIndex = stream.bitIndex;

                stream.bitIndex = totalBitIndex;
                stream.WriteUint(written, 32);

                stream.bitIndex = finalOriginalIndex;

                indexer.isDone = true;

                return indexer;
            }

            indexer.totalIndex++;
        }

        //to prevent variable name clashes
        {
            uint written = indexer.writeCount - originalWriteCount;

            //write final amount into buffer if all was successful
            int finalOriginalIndex = stream.bitIndex;

            stream.bitIndex = totalBitIndex;
            stream.WriteUint(written, 32);

            stream.bitIndex = finalOriginalIndex;

            indexer.isDone = true;

            return indexer;
        }
    }

    public void CacheBits(int tick = 0)
    {
        int entityCount = entities.Count;

        for (uint i = 0; i < entityCount; i++)
        {
            Entity item = entities[(int)i];

            //check whether a full update is due
            if (item.tick != tick)
            {
                //check if item has partial data to send
                if (!item.isNew && item.dirtyFlag > 0)
                {
                    int length = item.GetBitLengthPartial();

                    BitStream stream = new BitStream(length);
                    item.WriteToStreamPartial(ref stream);

                    BitCache cache = new BitCache();
                    cache.stream = stream;

                    bitCacheLookup.Place(cache, (int)item.id);
                }
            }
            else
            {
                if (!item.isNew)
                {
                    int length = item.GetBitLength();

                    BitStream stream = new BitStream(length);
                    item.WriteToStream(ref stream);

                    BitCache cache = new BitCache();
                    cache.stream = stream;

                    bitCacheLookup.Place(cache, (int)item.id);
                }
            }
        }
    }

    //writes all replication data for unreliable sources using cached bits, with multipass for loops
    public WriteReplicationIndexer WriteCachedReplicationDataIndexed(ref BitStream stream, ref CullingStack cullingStack, WriteReplicationIndexer previousIndexer = null, int tick = 0, int limit = 0)
    {
        int maxBits = Settings.BUFFER_LIMIT * 8;

        WriteReplicationIndexer indexer = previousIndexer;

        if (indexer == null)
        {
            indexer = new WriteReplicationIndexer();
        }

        //write 0 destroys on unreliable data
        stream.WriteInt(0, Settings.MAX_ENTITY_BITS);

        //int totalCount = entities.Count + rpcManager.rpcsToSend.Count;
        int entityCount = entities.Count;

        //set hard entity limit from priority (if there is one)
        int entityLimit = entityCount + 1;

        if (limit > 0)
        {
            entityLimit = limit;
        }

        int totalBitIndex = stream.bitIndex;
        stream.WriteInt(0, 32);

        uint originalWriteCount = indexer.writeCount;

        for (uint i = indexer.totalIndex; i < entityCount; i++)
        {
            Entity item = entities[(int)i];

            bool cullingTest = cullingStack.ApplyCulling(item);

            //check culling filter
            if (!cullingTest)
            {
                continue;
            }

            int size = item.GetBitLength() + 3;

            //check for overflow
            if (stream.bitIndex + size > maxBits)
            {
                //limit reached, rewrite total count
                int originalIndex = stream.bitIndex;

                stream.bitIndex = totalBitIndex;
                stream.WriteUint(indexer.writeCount - originalWriteCount, 32);

                stream.bitIndex = originalIndex;
                return indexer;
            }

            //check whether a full update is due
            if (item.tick != tick)
            {
                //check if item has partial data to send
                if (!item.isNew && item.dirtyFlag > 0)
                {
                    stream.WriteBits((int)EReplication.UPDATE_PARTIAL, 3);

                    BitCache cache = bitCacheLookup.Grab((int)item.id);
                    stream.WriteBytes(cache.stream.buffer, cache.stream.bitIndex);

                    indexer.writeCount++;
                }
            }
            else
            {
                if (!item.isNew)
                {
                    stream.WriteBits((int)EReplication.UPDATE, 3);

                    BitCache cache = bitCacheLookup.Grab((int)item.id);
                    stream.WriteBytes(cache.stream.buffer, cache.stream.bitIndex);

                    indexer.writeCount++;
                }
            }

            //limit reached
            if (indexer.writeCount >= entityLimit)
            {
                uint written = indexer.writeCount - originalWriteCount;

                //write final amount into buffer if all was successful
                int finalOriginalIndex = stream.bitIndex;

                stream.bitIndex = totalBitIndex;
                stream.WriteUint(written, 32);

                stream.bitIndex = finalOriginalIndex;

                indexer.isDone = true;

                return indexer;
            }

            indexer.totalIndex++;
        }

        //to prevent variable name clashes
        {
            uint written = indexer.writeCount - originalWriteCount;

            //write final amount into buffer if all was successful
            int finalOriginalIndex = stream.bitIndex;

            stream.bitIndex = totalBitIndex;
            stream.WriteUint(written, 32);

            stream.bitIndex = finalOriginalIndex;

            indexer.isDone = true;

            return indexer;
        }
    }

    public bool ReadyToWriteReliableData(bool forceCreate = false)
    {
        if (toDestroy.Count > 0)
        {
            return true;
        }

        int entityCount = entities.Count;

        if (forceCreate && entityCount > 0)
        {
            return true;
        }

        for (int i = 0; i < entityCount; i++)
        {
            Entity item = entities[i];

            if (item.isNew)
            {
                return true;
            }
        }

        return rpcManager.ReadyToWriteReliableData();
    }

    //wrapper function for reliable states
    public WriteReplicationIndexer WriteReplicationDataReliableIndexedInitial(ref BitStream stream, WriteReplicationIndexer previousIndexer = null)
    {
        return WriteReplicationDataReliableIndexed(ref stream, previousIndexer, true);
    }

    //writes all replication data for reliable sources, with multipass for loops
    public WriteReplicationIndexer WriteReplicationDataReliableIndexed(ref BitStream stream, WriteReplicationIndexer previousIndexer = null, bool forceCreate = false)
    {
        int maxBits = Settings.BUFFER_LIMIT * 8;

        WriteReplicationIndexer indexer = previousIndexer;

        if (indexer == null)
        {
            indexer = new WriteReplicationIndexer();
        }

        uint destroyCount = (uint)toDestroy.Count;

        //this is temporary, a full packet will trigger this to be rewritten
        int destroyBitIndex = stream.bitIndex;
        stream.WriteUint(destroyCount - indexer.destroyIndex, Settings.MAX_ENTITY_BITS);

        uint originalDestroyIndex = indexer.totalIndex;

        //write all recently destroyed objects
        for (uint i = indexer.destroyIndex; i < destroyCount; i++)
        {
            int size = 35;

            //check for overflow
            if (stream.bitIndex + size > maxBits)
            {
                //limit reached, rewrite destroy count
                int originalindex = stream.bitIndex;

                stream.bitIndex = destroyBitIndex;
                stream.WriteUint(i - originalDestroyIndex, Settings.MAX_ENTITY_BITS);

                stream.bitIndex = originalindex;
                return indexer;
            }

            stream.WriteBits((int)EReplication.DESTROY, 3);
            stream.WriteUint(toDestroy[(int)i], Settings.MAX_ENTITY_BITS);

            indexer.destroyIndex++;
        }

        //int totalCount = entities.Count + rpcManager.rpcsToSend.Count;
        uint entityCount = (uint)entities.Count;

        int totalBitIndex = stream.bitIndex;
        stream.WriteInt(0, 32);

        uint originalWriteCount = indexer.writeCount;

        //write all creations
        for (uint i = indexer.totalIndex; i < entityCount; i++)
        {
            Entity item = entities[(int)i];

            int size = item.GetBitLength() + 3;

            //check for overflow
            if (stream.bitIndex + size > maxBits)
            {
                //limit reached, rewrite total count
                int originalindex = stream.bitIndex;

                stream.bitIndex = totalBitIndex;
                stream.WriteUint(indexer.writeCount - originalWriteCount, 32);

                stream.bitIndex = originalindex;
                return indexer;
            }

            if (item.isNew || forceCreate)
            {
                bool oldState = item.isNew;

                stream.WriteBits((int)EReplication.CREATE, 3);

                if (forceCreate)
                {
                    item.isNew = true;
                }

                item.WriteToStream(ref stream);

                if (forceCreate)
                {
                    item.isNew = oldState;
                }

                indexer.writeCount++;
                indexer.createCount++;
            }

            indexer.totalIndex++;
        }

        uint written = indexer.writeCount - originalWriteCount;

        rpcManager.WriteReplicationDataReliableIndexed(ref stream, ref indexer, totalBitIndex, indexer.createCount, written);

        if (indexer.isDone)
        {
            //write final amount into buffer if all was successful
            int finalOriginalIndex = stream.bitIndex;

            stream.bitIndex = totalBitIndex;
            stream.WriteUint(indexer.writeCount - originalWriteCount, 32);

            stream.bitIndex = finalOriginalIndex;
        }

        return indexer;
    }

    //reset all entity write states so that the flags can be accurately calculated for the next tick
    public void ClearDirtyFlags()
    {
        int count = entities.Count;

        for (int i = 0; i < count; i++)
        {
            entities[i].dirtyFlag = 0;
        }
    }
}
