using System.Collections.Generic;

public class DistanceCulling : Culling
{
    public float maxDistance = 10.0f;
    float maxSqrDistance = 100.0f;

    //using squared distance is much more efficient
    public void CalculateSquareDistance()
    {
        maxSqrDistance = maxDistance * maxDistance;
    }

    public override bool ApplyCulling(Entity entity, List<PlayerEntity> group)
    {
        TransformEntity transformEntity = entity as TransformEntity;

        //this isn't a transform entity, it will pass by default
        if (transformEntity == null)
        {
            return true;
        }

        int count = group.Count;

        if (count == 0)
        {
            return true;
        }

        //groups apply OR logic, only one distance check has to succeed
        for (int i = 0; i < count; i++)
        {
            PlayerEntity player = group[i];

            float sqrDistance = (transformEntity.transform.position - player.transform.position).sqrMagnitude;

            if (sqrDistance < maxSqrDistance)
            {
                return true;
            }
        }

        return false;
    }
}
