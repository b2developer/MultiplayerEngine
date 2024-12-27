using UnityEngine;

public class Follow : MonoBehaviour
{
    public Transform target;

    void Start()
    {
        
    }

    public void Tick()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}
