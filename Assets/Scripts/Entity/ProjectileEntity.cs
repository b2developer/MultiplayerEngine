using System.Collections.Generic;
using UnityEngine;

public class ProjectileEntity : TransformEntity
{
    public EntityManager entityManager;

    public uint ownerId = 0;

    public float radius = 0.05f;
    public Vector3 velocity = Vector3.zero;
    
    public float speed = 5.0f;
    public float lifetime = 5.0f;

    public float explosionRadius = 3.25f;
    public float knockback = 0.0f;

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

    public void SimulateExplosion()
    {
        List<PlayerEntity> players = new List<PlayerEntity>();

        int count = entityManager.entities.Count;

        for (int i = 0; i < count; i++)
        {
            PlayerEntity item = entityManager.entities[i] as PlayerEntity;

            if (item == null)
            {
                continue;
            }

            players.Add(item);
        }

        foreach (PlayerEntity player in players)
        {
            Vector3 relative = player.transform.position - transform.position;
            float distance = relative.magnitude;

            if (distance > explosionRadius)
            {
                continue;
            }

            float lerp = distance / explosionRadius;
            lerp = 1.0f - lerp;

            //50% base damage up to 100% damage
            float modifier = Mathf.Lerp(0.5f, 1.0f, lerp);

            player.noGroundTimer = PlayerEntity.NO_GROUND_TIME;
            player.airTimer = PlayerEntity.AIR_TIME;

            if (distance > 0.0f)
            {
                Vector3 normalised = relative / distance;
                player.body.linearVelocity += knockback * modifier * normalised;
            }
        }
    }

    //no fixed update here

    public override void Tick()
    {
        bool isDestroyed = false;

        //lifetime script
        //------------
        Vector3 movement = velocity * Time.fixedDeltaTime;
        float magnitude = movement.magnitude;

        RaycastHit[] hitList = Physics.SphereCastAll(transform.position, radius, movement / magnitude, magnitude);
        
        for (int i = 0; i < hitList.Length; i++)
        {
            RaycastHit item = hitList[i];

            Entity entity = item.transform.GetComponent<Entity>();

            if (entity == null)
            {
                if (!isDestroyed)
                {
                    entityManager.DestroyServer(id);
                    isDestroyed = true;
                }

                transform.position = item.point + item.normal * radius;
                SimulateExplosion();

                break;
            }
            else
            {
                if (entity.id != ownerId)
                {
                    if (!isDestroyed)
                    {
                        entityManager.DestroyServer(id);
                        isDestroyed = true;
                    }

                    transform.position = item.point + item.normal * radius;
                    SimulateExplosion();

                    break;
                }
            }
        }

        transform.position += velocity * Time.fixedDeltaTime;

        lifetime -= Time.fixedDeltaTime;

        if (lifetime <= 0.0f)
        {
            lifetime = 0.0f;

            if (!isDestroyed)
            {
                entityManager.DestroyServer(id);
                isDestroyed = true;
            }
        }
        //------------

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