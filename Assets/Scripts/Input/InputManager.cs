using UnityEngine;

//wrapper class for input sample, essentially allows the client to assume control over their copy
public class InputManager : MonoBehaviour
{
    public CameraController cameraController;

    public bool menuOverride = false;
    public bool fuzz = false;

    public virtual void Initialise()
    {

    }

    public virtual InputSample GetInputSample()
    {
        return null;
    }

    public virtual void PerFrameUpdate()
    {

    }

    public virtual void Tick()
    {

    }
}
