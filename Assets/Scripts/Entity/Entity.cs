using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EObject
{
    CUBE = 0,
    BALL = 1,
    SPINNER = 2,
    CHAMELEON = 3,
    PLAYER = 4,
    PROJECTILE = 5,
    MAX = 6,
}

//double inheritance? ew
public class Entity : MonoBehaviour, IComparable<Entity>
{
    //spawning and full update data
    public bool isNew = true;
    public int tick = 0;

    //priority data
    public float priority = 0.0f;

    //flag to let the client opt out of recieving state data
    public bool accept = true;

    public uint id = 0;
    public EObject type;

    //flag that marks what values have changed since last tick
    public int dirtyFlag = 0;
    public int dirtyFlagLength = 0;

    public float idleTime = 0.0f;

    public virtual void Initialise()
    {
        UpdateManager.instance.entityFunction += Tick;
        dirtyFlagLength = 0;
    }

    public virtual void SetActive(bool state)
    {
        //only call SetActive() if the state is different from the game object's internal state
        if (gameObject.activeSelf != state)
        {
            gameObject.SetActive(state);
        }
    }

    public virtual void WriteToStream(ref BitStream stream)
    {
        stream.WriteUint(id, Settings.MAX_ENTITY_BITS);
        stream.WriteInt((int)type, Settings.MAX_TYPE_BITS);
    }

    public virtual void ReadFromStream(ref BitStream stream)
    {
        id = stream.ReadUint(Settings.MAX_ENTITY_BITS);
        type = (EObject)stream.ReadInt(Settings.MAX_TYPE_BITS);

        idleTime = 0.0f;
    }

    public virtual int GetBitLength()
    {
        int total = Settings.MAX_ENTITY_BITS + Settings.MAX_TYPE_BITS + 1;

        return total;
    }

    public virtual void WriteToStreamPartial(ref BitStream stream)
    {
        stream.WriteUint(id, Settings.MAX_ENTITY_BITS);
        stream.WriteInt((int)type, Settings.MAX_TYPE_BITS);

        if (dirtyFlagLength > 0)
        {
            stream.WriteInt(dirtyFlag, dirtyFlagLength);
        }
    }

    public virtual void ReadFromStreamPartial(ref BitStream stream)
    {
        id = stream.ReadUint(Settings.MAX_ENTITY_BITS);
        type = (EObject)stream.ReadInt(Settings.MAX_TYPE_BITS);

        if (dirtyFlagLength > 0)
        {
            dirtyFlag = stream.ReadInt(dirtyFlagLength);
        }

        idleTime = 0.0f;
    }

    public virtual int GetBitLengthPartial()
    {
        int total = Settings.MAX_ENTITY_BITS + Settings.MAX_TYPE_BITS;

        return total + dirtyFlagLength;
    }

    public virtual void Tick()
    {

    }

    public bool TickTimeout()
    {
        idleTime += Time.fixedDeltaTime;
        return idleTime > Settings.ENTITY_TIMEOUT_TIME;
    }

    public virtual void SetPriority(PlayerEntity player)
    {
        priority = 0.0f;
    }

    public int CompareTo(Entity other)
    {
        if (other == null)
        {
            return 1;
        }

        return -priority.CompareTo(other.priority);
    }
}