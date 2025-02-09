using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipClientEntity : TransformEntity
{
    public float battery = 0.0f;

    public Transform[] batteryTransforms;

    public override void Initialise()
    {
        base.Initialise();
    }

    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);

        stream.WriteFloat(battery, 32);
    }

    public override void ReadFromStream(ref BitStream stream)
    {
        base.ReadFromStream(ref stream);

        battery = stream.ReadFloat(32);
    }

    public override int GetBitLength()
    {
        int total = base.GetBitLength();

        total += 32;

        return total;
    }

    public override void ReadFromStreamPartial(ref BitStream stream)
    {
        base.ReadFromStreamPartial(ref stream);

        battery = stream.ReadFloat(32);
    }

    public override void WriteToStreamPartial(ref BitStream stream)
    {
        base.WriteToStreamPartial(ref stream);

        stream.WriteFloat(battery, 32);
    }

    public override int GetBitLengthPartial()
    {
        int total = base.GetBitLengthPartial();

        total += 32;

        return total;
    }

    public override void Tick()
    {
        if (interpolationFilter == null)
        {
            position.vector = transform.position;
            rotation.quaternion = transform.rotation;

            if (position.vector.x != previousPosition.x)
            {
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.POSITION_X;
            }

            if (position.vector.y != previousPosition.y)
            {
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.POSITION_Y;
            }

            if (position.vector.z != previousPosition.z)
            {
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.POSITION_Z;
            }

            if (rotation.quaternion.x != previousRotation.x)
            {
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_X;
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_W;
            }

            if (rotation.quaternion.y != previousRotation.y)
            {
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_Y;
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_W;
            }

            if (rotation.quaternion.z != previousRotation.z)
            {
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_Z;
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_W;
            }

            if (rotation.quaternion.w != previousRotation.w)
            {
                dirtyFlag = dirtyFlag | (int)ETransformEntityProperties.ROTATION_W;
            }

            previousPosition = position.vector;
            previousRotation = rotation.quaternion;
        }
    }

    public new void Update()
    {
        if (interpolationFilter != null)
        {
            interpolationFilter.Update(transform, Time.deltaTime);
        }

        foreach (Transform bt in batteryTransforms)
        {
            bt.localScale = new Vector3(battery, bt.localScale.y, bt.localScale.z);
        }
    }

    public override void SetPriority(PlayerEntity player)
    {
        //simple inverse square distance with division by zero prevention
        Vector3 relative = player.transform.position - transform.position;
        float sqrDistance = relative.sqrMagnitude;

        float cap = Mathf.Max(sqrDistance, 1.0f);
        float inverse = 1.0f / cap;

        priority = inverse;
    }
}