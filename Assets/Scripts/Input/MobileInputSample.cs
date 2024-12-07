using UnityEngine;

public class MobileInputSample : InputSample
{
    public FixedPoint joystickX;
    public FixedPoint joystickY;

    public override void Initialise()
    {
        type = EInput.MOBILE;

        base.Initialise();

        joystickX = new FixedPoint(-1.0f, 1.0f, 0.125f);
        joystickY = new FixedPoint(-1.0f, 1.0f, 0.125f);
    }

    public override Vector2 GetMovementVector()
    {
        Vector2 virtualJoystickVector = new Vector2(joystickX.value, joystickY.value);

        //make sure inputs are sanitised
        if (virtualJoystickVector.sqrMagnitude > 1.0f)
        {
            virtualJoystickVector.Normalize();
        }

        return virtualJoystickVector;
    }

    public override Vector3 GetLookVector()
    {
        return MathExtension.DirectionFromYawPitch(yaw, pitch);
    }

    //entropy encoding: 0 for no left / right, 1 for left or right followed by the identifier bit
    public override void WriteToStream(ref BitStream stream)
    {
        base.WriteToStream(ref stream);

        joystickX.WriteFixedPoint(ref stream);
        joystickY.WriteFixedPoint(ref stream);
    }

    public override void ReadFromStream(ref BitStream stream)
    {
        base.ReadFromStream(ref stream);

        joystickX.ReadFixedPoint(ref stream);
        joystickY.ReadFixedPoint(ref stream);
    }

    public override InputSample Clone()
    {
        MobileInputSample copy = new MobileInputSample();

        copy.timestamp = timestamp;

        copy.jump = jump.Clone();
        copy.fire = fire.Clone();

        copy.yaw = yaw;
        copy.pitch = pitch;

        copy.joystickX = joystickX.Clone();
        copy.joystickY = joystickY.Clone();

        return copy;
    }

    public override void Print()
    {
        Debug.Log("Joystick X: " + joystickX.value.ToString());
        Debug.Log("Joystick Y: " + joystickY.value.ToString());
        Debug.Log("Jump: " + jump.state.ToString());
        Debug.Log("Yaw: " + yaw.ToString());
        Debug.Log("Pitch: " + pitch.ToString());
    }
}
