using System.Collections.Generic;

public class DistanceCulling : Culling
{
    public delegate LookupTable<TransformCache> GetTransformCacheFunc();

    public float maxDistance = 10.0f;
    public float maxSqrDistance = 100.0f;

    public GetTransformCacheFunc getTransformCacheCallback;
    public LookupTable<TransformCache> transformCacheLookup = null;

    //using squared distance is much more efficient
    public void CalculateSquareDistance()
    {
        maxSqrDistance = maxDistance * maxDistance;
    }

    public override bool ApplyCulling(Entity entity, List<PlayerEntity> group)
    {
        transformCacheLookup = getTransformCacheCallback();

        TransformEntity transformEntity = entity as TransformEntity;

        //this isn't a transform entity, it will pass by default
        if (transformEntity == null)
        {
            return true;
        }

        TransformCache transformCache = transformCacheLookup.Grab((int)transformEntity.id);

        int count = group.Count;

        if (count == 0)
        {
            return true;
        }

        //groups apply OR logic, only one distance check has to succeed
        for (int i = 0; i < count; i++)
        {
            PlayerEntity player = group[i];

            TransformCache playerCache = transformCacheLookup.Grab((int)player.id);

            float sqrDistance = (transformCache.position - playerCache.position).sqrMagnitude;

            if (sqrDistance < maxSqrDistance)
            {
                return true;
            }
        }

        return false;
    }

    public override Culling Clone()
    {
        DistanceCulling distanceCulling = new DistanceCulling();

        distanceCulling.mode = mode;
        distanceCulling.maxDistance = maxDistance;
        distanceCulling.maxSqrDistance = maxSqrDistance;

        distanceCulling.getTransformCacheCallback = getTransformCacheCallback;

        return distanceCulling;
    }
}
