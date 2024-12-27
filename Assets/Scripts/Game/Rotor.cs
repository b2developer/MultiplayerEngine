using UnityEngine;

public class Rotor : MonoBehaviour
{
    public Vector3 speed;

    void Start()
    {
        
    }

    void Update()
    {
        transform.Rotate(speed * Time.deltaTime);
    }
}
