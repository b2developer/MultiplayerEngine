using UnityEngine;

public class OrthoSprite : MonoBehaviour
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
        relative.Normalize();

        transform.rotation = Quaternion.LookRotation(relative, Camera.main.transform.up);
    }
}
