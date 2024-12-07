using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChameleonEntity : TransformEntity
{
    public MeshRenderer meshRenderer;

    public override void Initialise()
    {
        base.Initialise();
    }

    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);
    }

    public override void ReadFromStream(ref BitStream stream)
    {
        base.ReadFromStream(ref stream);
    }

    public override int GetBitLength()
    {
        int total = base.GetBitLength();

        return total;
    }

    public override void ReadFromStreamPartial(ref BitStream stream)
    {
        base.ReadFromStreamPartial(ref stream);
    }

    public override void WriteToStreamPartial(ref BitStream stream)
    {
        base.WriteToStreamPartial(ref stream);
    }

    public override int GetBitLengthPartial()
    {
        int total = base.GetBitLengthPartial();

        return total;
    }

    //no fixed update here

    public void ChangeColor(float r, float g, float b)
    {
        float a = meshRenderer.material.color.a;
        meshRenderer.material.color = new Color(r, g, b, a);
    }

    public override void Tick()
    {
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