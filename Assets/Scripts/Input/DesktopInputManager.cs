using System.Security.Permissions;
using UnityEngine;

//wrapper class for input sample, essentially allows the client to assume control over their copy
public class DesktopInputManager : InputManager
{
    public DesktopInputSample sample;

    public bool hasFocus = false;

    public override void Initialise()
    {
        sample = new DesktopInputSample();
        sample.Initialise();

        cameraController.inputType = EInput.DESKTOP;
        cameraController.menuManager.inputType = EInput.DESKTOP;
    }

    public override InputSample GetInputSample()
    {
        return sample;
    }

    public override void PerFrameUpdate()
    {
        //inputs must be sampled every frame
        sample.left.Poll(menuOverride, fuzz);
        sample.right.Poll(menuOverride, fuzz);
        sample.forward.Poll(menuOverride, fuzz);
        sample.backward.Poll(menuOverride, fuzz);
        sample.jump.Poll(menuOverride, fuzz);
        
        cameraController.Poll();

        bool hasFocusCurrent = Cursor.lockState == CursorLockMode.Locked;

        //require the fire button to be released before it can be triggered
        if (hasFocus != hasFocusCurrent)
        {
            sample.fire.requireRelease = true;
        }

        hasFocus = hasFocusCurrent;

        sample.fire.Poll(menuOverride, fuzz);

        sample.yaw = cameraController.yaw;

        sample.pitch = 0.0f;

        if (!cameraController.isThirdPerson)
        {
            sample.pitch = cameraController.pitch;
        }
    }

    public override void Tick()
    {
        sample.left.Reset();
        sample.right.Reset();
        sample.forward.Reset();
        sample.backward.Reset();
        sample.jump.Reset();
        sample.fire.Reset();

        sample.timestamp++;

        if (sample.timestamp >= Settings.MAX_INPUT_INDEX)
        {
            sample.timestamp -= Settings.MAX_INPUT_INDEX;
        }
    }
}
