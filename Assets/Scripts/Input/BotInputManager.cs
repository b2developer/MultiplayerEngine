using UnityEngine;

public class BotInputManager : InputManager
{
    public DesktopInputSample sample;

    public bool hasFocus = false;

    public override void Initialise()
    {
        sample = new DesktopInputSample();
        sample.Initialise();

        cameraController.inputType = EInput.DESKTOP;
    }

    public override InputSample GetInputSample()
    {
        return sample;
    }

    public override void PerFrameUpdate()
    {
        //inputs must be sampled every frame
        sample.left.PollAutomatic(false, menuOverride, fuzz);
        sample.right.PollAutomatic(false, menuOverride, fuzz);
        sample.forward.PollAutomatic(false, menuOverride, fuzz);
        sample.backward.PollAutomatic(false, menuOverride, fuzz);
        sample.jump.PollAutomatic(false, menuOverride, fuzz);

        cameraController.Poll();

        bool hasFocusCurrent = Cursor.lockState == CursorLockMode.Locked;

        //require the fire button to be released before it can be triggered
        if (hasFocus != hasFocusCurrent)
        {
            sample.fire.requireRelease = true;
        }

        hasFocus = hasFocusCurrent;

        sample.fire.PollAutomatic(false, menuOverride, fuzz);

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
