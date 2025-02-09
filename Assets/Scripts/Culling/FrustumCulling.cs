using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class FrustumCulling : Culling
{
    public delegate LookupTable<TransformCache> GetTransformCacheFunc();

    public static float FIELD_OF_VIEW = 60.0f;
    public static float ASPECT_RATIO = 1920.0f / 1080.0f;
    public static float INFLATION = 10.0f;
    public float fovH = 0.0f;

    public GetTransformCacheFunc getTransformCacheCallback;
    public LookupTable<TransformCache> transformCacheLookup = null;

    //there is a weird forumla for this
    public void CalculateHorizontalFieldOfView()
    {
        float fovRadian = FIELD_OF_VIEW * Mathf.Deg2Rad;
        fovH = 2.0f * Mathf.Atan(Mathf.Tan(fovRadian / 2.0f) * ASPECT_RATIO) * Mathf.Rad2Deg;
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

        ColliderCache cache = GetColliderCache(transformEntity);

        if (cache.isEmpty)
        {
            return true;
        }

        int count = group.Count;

        if (count == 0)
        {
            return true;
        }

        TransformCache transformCache = transformCacheLookup.Grab((int)transformEntity.id);

        //groups apply OR logic, only one distance check has to succeed
        for (int i = 0; i < count; i++)
        {
            PlayerEntity player = group[i];

            Plane[] planes = GetFrustumPlanes(player);
            int successCount = 0;

            for (int j = 0; j < 4; j++)
            {
                Plane plane = planes[j];
                bool success = false;

                Vector3 centre = transformCache.TransformPoint(Vector3.zero);
                float radius = transformCache.scale.x * cache.boundingRadius;

                Debug.DrawLine(centre, centre + Vector3.up * radius, Color.red);
                Debug.DrawLine(centre, centre + Vector3.down * radius, Color.red);
                Debug.DrawLine(centre, centre + Vector3.left * radius, Color.red);
                Debug.DrawLine(centre, centre + Vector3.right * radius, Color.red);
                Debug.DrawLine(centre, centre + Vector3.forward * radius, Color.red);
                Debug.DrawLine(centre, centre + Vector3.back * radius, Color.red);

                if (MathExtension.SphereInsidePlane(plane, centre, radius))
                {
                    success = true;
                    successCount++;
                    continue;
                }

                /*
                for (int k = 0; k < cache.spheres.Length; k++)
                {
                    Vector3 centre = transformCache.TransformPoint(cache.sphereCentres[k]);
                    float radius = transformCache.scale.x * cache.sphereRadiuses[k];

                    if (MathExtension.SphereInsidePlane(plane, centre, radius))
                    {
                        success = true;
                        successCount++;
                        continue;
                    }
                }

                if (!success)
                {
                    for (int k = 0; k < cache.boxes.Length; k++)
                    {
                        Vector3 centre = transformCache.TransformPoint(cache.boxCentres[k]);
                        Vector3 size = new Vector3(transformCache.scale.x * cache.boxSizes[k].x, transformCache.scale.y * cache.boxSizes[k].y, transformCache.scale.z * cache.boxSizes[k].z);

                        if (MathExtension.BoxInsidePlane(plane, centre, transformCache.rotation, size))
                        {
                            success = true;
                            successCount++;
                            continue;
                        }
                    }
                }

                if (!success)
                {
                    for (int k = 0; k < cache.capsules.Length; k++)
                    {
                        Vector3 centre = transformCache.TransformPoint(cache.capsuleCentres[k]);
                        float radius = transformCache.scale.x * cache.capsuleRadiuses[k];
                        Vector3 direction = transformCache.TransformDirection(Vector3.up);
                        float height = transformCache.scale.x * cache.capsuleHeights[k];

                        if (MathExtension.CapsuleInsidePlane(plane, centre, radius, direction, height))
                        {
                            success = true;
                            successCount++;
                            continue;
                        }
                    }
                }
                */

                if (!success)
                {
                    break;
                }
            }

            if (successCount >= 4)
            {
                return true;
            }
        }

        return false;
    }

    public Plane[] GetFrustumPlanes(PlayerEntity player)
    {
        transformCacheLookup = getTransformCacheCallback();

        TransformCache transformCache = transformCacheLookup.Grab((int)player.id);

        Plane[] planes = new Plane[4];

        Vector3 forward = transformCache.rotation * Vector3.forward;

        Vector3 look = forward;
        
        for (int i = 0; i < 2; i++)
        {
            look = forward;

            int xSign = (i & 0x1) * 2 - 1;
            float xOffset = -90.0f * xSign;

            Vector3 normal = MathExtension.RotateWithYawPitch(look, (fovH * 0.5f + INFLATION) * xSign + xOffset, 0.0f);

            Plane plane = new Plane(transformCache.position, normal);
            planes[i] = plane;
        }

        for (int i = 0; i < 2; i++)
        {
            look = forward;

            int ySign = (i & 0x1) * 2 - 1;
            float yOffset = -90.0f * ySign;

            Vector3 normal = MathExtension.RotateWithYawPitch(look, 0.0f, (FIELD_OF_VIEW * 0.5f + INFLATION) * ySign + yOffset);

            Plane plane = new Plane(transformCache.position, normal);
            planes[i + 2] = plane;
        }

        return planes;
    }

    public ColliderCache GetColliderCache(TransformEntity entity)
    {
        if (entity.colliderCache == null)
        {
            entity.colliderCache = new ColliderCache();
            entity.colliderCache.Generate(entity.gameObject);
        }

        return entity.colliderCache;
    }

    public override Culling Clone()
    {
        FrustumCulling frustumCulling = new FrustumCulling();

        frustumCulling.mode = mode;
        frustumCulling.fovH = fovH;
        frustumCulling.getTransformCacheCallback = getTransformCacheCallback;

        return frustumCulling;
    }
}
