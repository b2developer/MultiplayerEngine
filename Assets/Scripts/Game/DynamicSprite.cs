using UnityEngine;

public class DynamicSprite : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        if (Camera.main == null)
        {
            return;
        }

        Vector3 relative = transform.position - Camera.main.transform.position;
        relative.y = 0.0f;
        relative.Normalize();

        transform.rotation = Quaternion.LookRotation(relative);
    }
}
