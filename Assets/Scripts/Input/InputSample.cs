using UnityEngine;

public enum EInput
{
    DESKTOP = 0,
    MOBILE = 1,
}

public enum EButtonState
{
    ON_PRESS = 0,
    PRESSED = 1,
    ON_RELEASE = 2,
    RELEASED = 3,
}

public class Button
{
    public KeyCode keyCode;
    public EButtonState state;
    public bool changeDetected = false;
    public bool requireRelease = false;

    public Button(KeyCode _keyCode)
    {
        keyCode = _keyCode;
        state = EButtonState.RELEASED;
    }

    public void Poll(bool menuOverride, bool fuzz = false)
    {
        if (changeDetected)
        {
            return;
        }

        bool keyState = Input.GetKey(keyCode);

        if (fuzz)
        {
            keyState = Random.Range(0, 2) == 1 ? true : false;
        }

        if (keyState && !menuOverride && !requireRelease)
        {
            if (state == EButtonState.ON_RELEASE)
            {
                changeDetected = true;
                state = EButtonState.RELEASED;
            }
            else if (state == EButtonState.RELEASED)
            {
                changeDetected = true;
                state = EButtonState.ON_PRESS;
            }
            else if (state == EButtonState.ON_PRESS)
            {
                changeDetected = true;
                state = EButtonState.PRESSED;
            }
        }
        else
        {
            //check that we weren't forces into a release
            if (!keyState)
            {
                requireRelease = false;
            }

            if (state == EButtonState.ON_PRESS)
            {
                changeDetected = true;
                state = EButtonState.PRESSED;
            }
            else if (state == EButtonState.PRESSED)
            {
                changeDetected = true;
                state = EButtonState.ON_RELEASE;
            }
            else if (state == EButtonState.ON_RELEASE)
            {
                changeDetected = true;
                state = EButtonState.RELEASED;
            }
        }
    }

    public void PollAutomatic(bool automatedState, bool menuOverride, bool fuzz = false)
    {
        if (changeDetected)
        {
            return;
        }

        if (fuzz)
        {
            automatedState = Random.Range(0, 2) == 1 ? true : false;
        }

        if (automatedState && !menuOverride && !requireRelease)
        {
            if (state == EButtonState.ON_RELEASE)
            {
                changeDetected = true;
                state = EButtonState.RELEASED;
            }
            else if (state == EButtonState.RELEASED)
            {
                changeDetected = true;
                state = EButtonState.ON_PRESS;
            }
            else if (state == EButtonState.ON_PRESS)
            {
                changeDetected = true;
                state = EButtonState.PRESSED;
            }
        }
        else
        {
            //check that we weren't forces into a release
            if (!automatedState)
            {
                requireRelease = false;
            }

            if (state == EButtonState.ON_PRESS)
            {
                changeDetected = true;
                state = EButtonState.PRESSED;
            }
            else if (state == EButtonState.PRESSED)
            {
                changeDetected = true;
                state = EButtonState.ON_RELEASE;
            }
            else if (state == EButtonState.ON_RELEASE)
            {
                changeDetected = true;
                state = EButtonState.RELEASED;
            }
        }
    }

    public void Reset()
    {
        changeDetected = false;
    }

    public Button Clone()
    {
        Button copy = new Button(keyCode);

        copy.state = state;
        copy.changeDetected = changeDetected;

        return copy;
    }
}

public class InputSample
{
    public EInput type;

    public int timestamp = 0;

    public float yaw = 0.0f;
    public float pitch = 0.0f;

    public Button jump;
    public Button fire;

    public virtual void Initialise()
    {
        jump = new Button(KeyCode.Space);
        fire = new Button(KeyCode.Mouse0);
    }

    public virtual Vector2 GetMovementVector()
    {
        return Vector2.zero;
    }

    public virtual Vector3 GetLookVector()
    {
        return MathExtension.DirectionFromYawPitch(yaw, pitch);
    }

    public virtual void WriteToStream(ref BitStream stream)
    {
        stream.WriteInt((int)type, 1);

        stream.WriteInt(timestamp, 16);

        stream.WriteFloat(yaw, 32);
        stream.WriteFloat(pitch, 32);

        stream.WriteInt((int)jump.state, 2);
        stream.WriteInt((int)fire.state, 2);
    }

    public virtual void ReadFromStream(ref BitStream stream)
    {
        timestamp = stream.ReadInt(16);

        yaw = stream.ReadFloat(32);
        pitch = stream.ReadFloat(32);

        byte jumpBit = stream.ReadBits(2)[0];
        jump.state = (EButtonState)jumpBit;

        byte fireBit = stream.ReadBits(2)[0];
        fire.state = (EButtonState)fireBit;
    }

    public virtual InputSample Clone()
    {
        InputSample copy = new InputSample();

        copy.yaw = yaw;
        copy.pitch = pitch;
        copy.timestamp = timestamp;

        return copy;
    }

    public virtual void Print()
    {
        Debug.Log("Yaw: " + yaw.ToString());
        Debug.Log("Pitch: " + pitch.ToString());
    }

    //mini registry for inputs (not worth it's own class)
    public static InputSample ConstructFromStream(ref BitStream stream)
    {
        EInput type = (EInput)stream.ReadInt(1);

        if (type == EInput.DESKTOP)
        {
            DesktopInputSample sample = new DesktopInputSample();

            sample.Initialise();
            sample.ReadFromStream(ref stream);

            return sample;
        }
        else if (type == EInput.MOBILE)
        {
            MobileInputSample sample = new MobileInputSample();

            sample.Initialise();
            sample.ReadFromStream(ref stream);

            return sample;
        }

        return null;
    }
}
