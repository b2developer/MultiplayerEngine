using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class FrustumCulling : Culling
{
    public static float FIELD_OF_VIEW = 60.0f;
    public static float ASPECT_RATIO = 1920.0f / 1080.0f;
    public static float INFLATION = 10.0f;
    public float fovH = 0.0f;
    
    //there is a weird forumla for this
    public void CalculateHorizontalFieldOfView()
    {
        float fovRadian = FIELD_OF_VIEW * Mathf.Deg2Rad;
        fovH = 2.0f * Mathf.Atan(Mathf.Tan(fovRadian / 2.0f) * ASPECT_RATIO) * Mathf.Rad2Deg;
    }

    public override bool ApplyCulling(Entity entity, List<PlayerEntity> group)
    {
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

                for (int k = 0; k < cache.spheres.Length; k++)
                {
                    Vector3 centre = transformEntity.transform.TransformPoint(cache.spheres[k].center);
                    float radius = transformEntity.transform.localScale.x * cache.spheres[k].radius;

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
                        Vector3 centre = transformEntity.transform.TransformPoint(cache.boxes[k].center);
                        Vector3 size = new Vector3(transformEntity.transform.localScale.x * cache.boxes[k].size.x, transformEntity.transform.localScale.y * cache.boxes[k].size.y, transformEntity.transform.localScale.z * cache.boxes[k].size.z);

                        if (MathExtension.BoxInsidePlane(plane, centre, transformEntity.transform.rotation, size))
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
                        Vector3 centre = transformEntity.transform.TransformPoint(cache.capsules[k].center);
                        float radius = transformEntity.transform.localScale.x * cache.capsules[k].radius;
                        Vector3 direction = transformEntity.transform.TransformDirection(Vector3.up);
                        float height = transformEntity.transform.localScale.x * cache.capsules[k].height;

                        if (MathExtension.CapsuleInsidePlane(plane, centre, radius, direction, height))
                        {
                            success = true;
                            successCount++;
                            continue;
                        }
                    }
                }

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
        Plane[] planes = new Plane[4];

        Vector3 look = player.transform.forward;
        
        for (int i = 0; i < 2; i++)
        {
            look = player.transform.forward;

            int xSign = (i & 0x1) * 2 - 1;
            float xOffset = -90.0f * xSign;

            Vector3 normal = MathExtension.RotateWithYawPitch(look, (fovH * 0.5f + INFLATION) * xSign + xOffset, 0.0f);

            Plane plane = new Plane(player.transform.position, normal);
            planes[i] = plane;
        }

        for (int i = 0; i < 2; i++)
        {
            look = player.transform.forward;

            int ySign = (i & 0x1) * 2 - 1;
            float yOffset = -90.0f * ySign;

            Vector3 normal = MathExtension.RotateWithYawPitch(look, 0.0f, (FIELD_OF_VIEW * 0.5f + INFLATION) * ySign + yOffset);

            Plane plane = new Plane(player.transform.position, normal);
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
}
