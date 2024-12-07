using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class InterpolationFilter
{
    public Vector3 previousPosition;
    public Quaternion previousRotation;

    public Vector3 currentPosition;
    public Quaternion currentRotation;

    public float timer = 0.0f;

    public bool enableRotation = true;

    public InterpolationFilter()
    {

    }

    public void SetPreviousState(Vector3 position, Quaternion rotation)
    {
        previousPosition = position;

        if (enableRotation)
        {
            previousRotation = rotation;
        }

        timer = 0.0f;
    }

    public void SetCurrentState(Vector3 position, Quaternion rotation)
    {
        currentPosition = position;

        if (enableRotation)
        {
            currentRotation = rotation;
        }

        Vector3 relative = currentPosition - previousPosition;
        float sqrDistance = relative.sqrMagnitude;

        //snap the position and rotation if the interpolation distance is too far (this was probably teleportation)
        if (sqrDistance > Settings.MAX_INTERPOLATION_DISTANCE * Settings.MAX_INTERPOLATION_DISTANCE)
        {
            previousPosition = position;

            if (enableRotation)
            {
                previousRotation = rotation;
            }
        }
    }

    public void Update(Transform transform, float deltaTime)
    {
        timer += deltaTime;

        if (timer > Settings.INTERPOLATION_PERIOD)
        {
            timer = Settings.INTERPOLATION_PERIOD;
        }

        float lerp = timer / Settings.INTERPOLATION_PERIOD;

        transform.position = Vector3.Lerp(previousPosition, currentPosition, lerp);

        if (enableRotation)
        {
            transform.rotation = Quaternion.Lerp(previousRotation, currentRotation, lerp);
        }
    }
}
