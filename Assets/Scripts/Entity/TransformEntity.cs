using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public enum ETransformEntityProperties
{
    POSITION_X = 1 << 0,
    POSITION_Y = 1 << 1,
    POSITION_Z = 1 << 2,

    ROTATION_X = 1 << 3,
    ROTATION_Y = 1 << 4,
    ROTATION_Z = 1 << 5,
    ROTATION_W = 1 << 6,
}

public class TransformEntity : Entity
{
    public InterpolationFilter interpolationFilter;

    //detect changes
    public Vector3 previousPosition;
    public Quaternion previousRotation;

    public FixedPoint3 position;
    public CompressedQuaternion rotation;

    //used to store collider information and prevent pointless re-calculation
    public ColliderCache colliderCache = null;

    public override void Initialise()
    {
        base.Initialise();
        dirtyFlagLength += 7;

        if (colliderCache == null)
        {
            colliderCache = new ColliderCache();
            colliderCache.Generate(gameObject);
        }
    }

    public override void SetActive(bool state)
    {
        bool wasActive = gameObject.activeSelf;
        base.SetActive(state);

        //fast-forward interpolation
        if (!wasActive && gameObject.activeSelf && interpolationFilter != null)
        {
            interpolationFilter.SetPreviousState(position.vector, rotation.quaternion);
            interpolationFilter.SetCurrentState(position.vector, rotation.quaternion);

            interpolationFilter.Update(transform, 0.0f);
        }
    }

    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);

        position.WriteFixedPoint(ref stream);
        rotation.WriteToStream(ref stream);
    }

    public override void ReadFromStream(ref BitStream stream)
    {
        if (interpolationFilter != null)
        {
            interpolationFilter.SetPreviousState(transform.localPosition, transform.localRotation);
        }

        base.ReadFromStream(ref stream);

        position.x.ReadFixedPoint(ref stream);
        position.y.ReadFixedPoint(ref stream);
        position.z.ReadFixedPoint(ref stream);

        float positionX = position.x.value;
        float positionY = position.y.value;
        float positionZ = position.z.value;

        if (accept)
        {
            position.vector = new Vector3(positionX, positionY, positionZ);
        }

        Quaternion originalRotation = rotation.quaternion;
        rotation.ReadFromStream(ref stream);

        if (!accept)
        {
            rotation.quaternion = originalRotation;
        }

        if (interpolationFilter != null)
        {
            interpolationFilter.SetCurrentState(position.vector, rotation.quaternion);
        }
        else
        {
            transform.localPosition = position.vector;
            transform.localRotation = rotation.quaternion;
        }    
    }

    public override int GetBitLength()
    {
        int total = base.GetBitLength();

        total += position.GetBitLength();
        total += rotation.GetBitLength();

        return total;
    }

    public override void WriteToStreamPartial(ref BitStream stream)
    {
        base.WriteToStreamPartial(ref stream);

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_X) > 0)
        {
            position.x.value = position.vector.x;
            position.x.WriteFixedPoint(ref stream);
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_Y) > 0)
        {
            position.y.value = position.vector.y;
            position.y.WriteFixedPoint(ref stream);
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_Z) > 0)
        {
            position.z.value = position.vector.z;
            position.z.WriteFixedPoint(ref stream);
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_X) > 0)
        {
            rotation.x.value = rotation.quaternion.x;
            rotation.x.WriteFixedPoint(ref stream);
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_Y) > 0)
        {
            rotation.y.value = rotation.quaternion.y;
            rotation.y.WriteFixedPoint(ref stream);
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_Z) > 0)
        {
            rotation.z.value = rotation.quaternion.z;
            rotation.z.WriteFixedPoint(ref stream);
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_W) > 0)
        {
            stream.WriteBool(rotation.quaternion.w > 0);
        }
    }

    public override void ReadFromStreamPartial(ref BitStream stream)
    {
        if (interpolationFilter != null)
        {
            interpolationFilter.SetPreviousState(transform.localPosition, transform.localRotation);
        }

        base.ReadFromStreamPartial(ref stream);

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_X) > 0)
        {
            position.x.ReadFixedPoint(ref stream);

            if (accept)
            {
                position.vector.x = position.x.value;
            }
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_Y) > 0)
        {
            position.y.ReadFixedPoint(ref stream);

            if (accept)
            {
                position.vector.y = position.y.value;
            }
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_Z) > 0)
        {
            position.z.ReadFixedPoint(ref stream);


            if (accept)
            {
                position.vector.z = position.z.value;
            }
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_X) > 0)
        {
            rotation.x.ReadFixedPoint(ref stream);

            if (accept)
            {
                rotation.quaternion.x = rotation.x.value;
            }
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_Y) > 0)
        {
            rotation.y.ReadFixedPoint(ref stream);

            if (accept)
            {
                rotation.quaternion.y = rotation.y.value;
            }
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_Z) > 0)
        {
            rotation.z.ReadFixedPoint(ref stream);

            if (accept)
            {
                rotation.quaternion.z = rotation.z.value;
            }
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_W) > 0)
        {
            float wSign = stream.ReadBool() ? 1.0f : -1.0f;

            if (accept)
            {
                //quaternion property of being a unit vector can be exploited here
                //w^2 = sqrt(1 - x^2 - y^2 - z^2)
                float w2 = 1.0f - rotation.x.value * rotation.x.value - rotation.y.value * rotation.y.value - rotation.z.value * rotation.z.value;
                float w = 0.0f;

                if (w2 > 0.0f)
                {
                    w = Mathf.Sqrt(w2) * wSign;
                }

                if (accept)
                {
                    rotation.quaternion.w = w;
                }
            }
        }

        if (interpolationFilter != null)
        {
            interpolationFilter.SetCurrentState(position.vector, rotation.quaternion);
        }
        else
        {
            transform.localPosition = position.vector;
            transform.localRotation = rotation.quaternion;
        }
    }

    public override int GetBitLengthPartial()
    {
        int total = base.GetBitLengthPartial();

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_X) > 0)
        {
            total += position.x.GetBitLength();
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_Y) > 0)
        {
            total += position.y.GetBitLength();
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.POSITION_Z) > 0)
        {
            total += position.z.GetBitLength();
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_X) > 0)
        {
            total += rotation.x.GetBitLength();
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_Y) > 0)
        {
            total += rotation.y.GetBitLength();
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_Z) > 0)
        {
            total += rotation.z.GetBitLength();
        }

        if ((dirtyFlag & (int)ETransformEntityProperties.ROTATION_W) > 0)
        {
            total += 1;
        }

        return total;
    }

    public override void Tick()
    {
        if (interpolationFilter == null)
        {
            position.vector = transform.localPosition;
            rotation.quaternion = transform.localRotation;

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

    public void Update()
    {
        if (interpolationFilter != null)
        {
            interpolationFilter.Update(transform, Time.deltaTime);
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

public class ColliderCache
{
    public bool isEmpty = false;

    public SphereCollider[] spheres;
    public BoxCollider[] boxes;
    public CapsuleCollider[] capsules;

    public Vector3[] sphereCentres;
    public float[] sphereRadiuses;

    public Vector3[] boxCentres;
    public Vector3[] boxSizes;

    public Vector3[] capsuleCentres;
    public float[] capsuleRadiuses;
    public float[] capsuleHeights;

    public float boundingRadius = 0.0f;

    public ColliderCache()
    {

    }

    public void Generate(GameObject gameObject)
    {
        spheres = gameObject.GetComponentsInChildren<SphereCollider>();
        boxes = gameObject.GetComponentsInChildren<BoxCollider>();
        capsules = gameObject.GetComponentsInChildren<CapsuleCollider>();

        isEmpty = spheres.Length == 0 && boxes.Length == 0 && capsules.Length == 0;

        float maxDistance = 0.0f;

        sphereCentres = new Vector3[spheres.Length];
        sphereRadiuses = new float[spheres.Length];

        for (int i = 0; i < spheres.Length; i++)
        {
            sphereCentres[i] = spheres[i].center;
            sphereRadiuses[i] = spheres[i].radius;

            float sphereDistance = sphereCentres[i].magnitude + sphereRadiuses[i];

            if (sphereDistance > maxDistance)
            {
                maxDistance = sphereDistance;
            }
        }

        boxCentres = new Vector3[boxes.Length];
        boxSizes = new Vector3[boxes.Length];

        for (int i = 0; i < boxes.Length; i++)
        {
            boxCentres[i] = boxes[i].center;
            boxSizes[i] = boxes[i].size;

            float boxDistance = boxCentres[i].magnitude + Mathf.Max(Mathf.Max(boxSizes[i].x, boxSizes[i].y), boxSizes[i].z);

            if (boxDistance > maxDistance)
            {
                maxDistance = boxDistance;
            }
        }

        capsuleCentres = new Vector3[capsules.Length];
        capsuleRadiuses = new float[capsules.Length];
        capsuleHeights = new float[capsules.Length];

        for (int i = 0; i < capsules.Length; i++)
        {
            capsuleCentres[i] = capsules[i].center;
            capsuleRadiuses[i] = capsules[i].radius;
            capsuleHeights[i] = capsules[i].height;

            float capsuleDistance = capsuleCentres[i].magnitude + capsuleRadiuses[i] + capsuleHeights[i];

            if (capsuleDistance > maxDistance)
            {
                maxDistance = capsuleDistance;
            }
        }

        boundingRadius = maxDistance;
    }
}