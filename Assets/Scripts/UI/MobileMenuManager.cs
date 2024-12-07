using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.XR;

public class MobileMenuManager : MenuManager
{
    public enum EOrientation
    {
        VERTICAL,
        HORIZONTAL,
    }

    public CanvasScaler scaler;

    public EOrientation currentOrientation;
    public DualLayout[] dualLayouts;

    public UnityEngine.UI.Button jumpButton;
    public UnityEngine.UI.Button fireButton;

    //joystick variables
    protected bool joystick = false;
    protected float joystickFadeLerp = 0.0f;
    public float joystickFadeTime = 0.0f;
    public RawImage[] joystickImages;
    protected float[] joystickOriginalAlphas;

    public RawImage joystickImage;
    public RawImage knobImage;
    public float originalJoystickSize = 400; //in pixels
    public float virtualJoystickSize = 1.5f; //in inches
    public float deadzone = 0.2f;

    public override void Initialise()
    {
        base.Initialise();

        joystickOriginalAlphas = new float[joystickImages.Length];

        for (int i = 0; i < joystickImages.Length; i++)
        {
            joystickOriginalAlphas[i] = joystickImages[i].color.a;
        }

        SetJoystickAlpha(0.0f);

        EOrientation orientation = DetectOrientation();
        SetOrientation(orientation);
    }

    public override void PerFrameUpdate()
    {
        UpdateOrientation();
        ScaleVirtualJoystick();
        UpdateJoystick();

        base.PerFrameUpdate();
    }

    public EOrientation DetectOrientation()
    {
        if (Screen.height > Screen.width)
        {
            return EOrientation.VERTICAL;
        }
        else
        {
            return EOrientation.HORIZONTAL;
        }
    }

    public void UpdateOrientation()
    {
        EOrientation orientation = DetectOrientation();

        if (currentOrientation != orientation)
        {
            currentOrientation = orientation;
            SetOrientation(orientation);
        }
    }

    public void SetOrientation(EOrientation orientation)
    {
        if (orientation == EOrientation.VERTICAL)
        {
            foreach (DualLayout layout in dualLayouts)
            {
                layout.SetVerticalLayout();
            }
        }
        else if (orientation == EOrientation.HORIZONTAL)
        {
            foreach (DualLayout layout in dualLayouts)
            {
                layout.SetHorizontalLayout();
            }
        }
    }

    public override void DisconnectButtonPressed()
    {
        client.SendDisconnect();

        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #else
        if (Settings.platformType == Settings.EPlatformType.ANDROID)
        {
            AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call<bool>("moveTaskToBack", true);
        }
        else if (Settings.platformType == Settings.EPlatformType.IOS)
        {
            Application.Quit();
        }
        #endif
    }

    public void PollGameButtons()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase != TouchPhase.Began)
            {
                continue;
            }

            //absorbed by ui
            if (MobileInputManager.IsTouchOverButton(touch, jumpButton))
            {
                JumpButtonPressed();
            }
            //absorbed by ui
            else if (MobileInputManager.IsTouchOverButton(touch, fireButton))
            {
                FireButtonPressed();
            }
        }
    }

    public void JumpButtonPressed()
    {
        buttonData.jump = true;
    }

    public void FireButtonPressed()
    {
        buttonData.fire = true;
    }

    public void ScaleVirtualJoystick()
    {
        float dpi = Screen.dpi;

        float screenScaleX = Mathf.Min(Screen.width, Screen.height) / scaler.referenceResolution.x;
        float screenScaleY = Mathf.Max(Screen.width, Screen.height) / scaler.referenceResolution.y;

        float screenScale = Mathf.Lerp(screenScaleX, screenScaleY, scaler.matchWidthOrHeight);

        float joystickInches = (originalJoystickSize * screenScale) / dpi;
        float ratio = virtualJoystickSize / joystickInches;

        joystickImage.rectTransform.localScale = Vector3.one * ratio;
    }

    public void PollVirtualJoystick()
    {
        //find touch in top half of screen
        ManagedTouch joystickDragData = null;

        RectTransform referenceTransform = jumpButton.GetComponent<RectTransform>();
        float jumpButtonY = referenceTransform.position.y;
        float jumpButtonYRatio = jumpButtonY / Screen.height;

        int count = touchData.touchList.Count;

        for (int i = 0; i < count; i++)
        {
            ManagedTouch item = touchData.touchList[i];

            if (currentOrientation == EOrientation.VERTICAL)
            {
                if (item.start.y < Screen.height * jumpButtonYRatio)
                {
                    joystickDragData = item;
                    break;
                }
            }
            else if (currentOrientation == EOrientation.HORIZONTAL)
            {
                if (item.start.x < Screen.width * 0.5f)
                {
                    joystickDragData = item;
                    break;
                }
            }
        }

        if (joystickDragData == null)
        {
            buttonData.joystick = Vector2.zero;
            joystick = false;
        }
        else
        {
            float dpi = Screen.dpi;

            float screenScaleX = Mathf.Min(Screen.width, Screen.height) / scaler.referenceResolution.x;
            float screenScaleY = Mathf.Max(Screen.width, Screen.height) / scaler.referenceResolution.y;

            float screenScale = Mathf.Lerp(screenScaleX, screenScaleY, scaler.matchWidthOrHeight);

            float joystickInches = (originalJoystickSize * screenScale) / dpi;
            float ratio = virtualJoystickSize / joystickInches;

            Vector2 joystickVector = joystickDragData.touch.position - joystickDragData.start;

            //scale to inches
            joystickVector.x /= dpi;
            joystickVector.y /= dpi;

            //normalize to virtual joystick size
            joystickVector /= virtualJoystickSize * ratio * 0.5f;

            if (joystickVector.sqrMagnitude > 1.0f)
            {
                joystickVector.Normalize();
            }

            buttonData.joystick = joystickVector;

            //set joystick visuals
            joystickImage.rectTransform.position = new Vector3(joystickDragData.start.x, joystickDragData.start.y, joystickImage.rectTransform.position.z);

            //'undo' all the changes, don't add ratio since this is a local position
            float pixelSize = originalJoystickSize * 0.5f;

            knobImage.rectTransform.localPosition = new Vector3(joystickVector.x * pixelSize, joystickVector.y * pixelSize, knobImage.rectTransform.localPosition.z);
            joystick = true;
        }
    }

    public void UpdateJoystick()
    {
        if (joystick)
        {
            if (joystickFadeLerp < 1.0f)
            {
                float inv = 1.0f / joystickFadeTime;
                joystickFadeLerp += inv * Time.deltaTime;

                if (joystickFadeLerp > 1.0f)
                {
                    joystickFadeLerp = 1.0f;
                }

                SetJoystickAlpha(joystickFadeLerp);
            }
        }
        else
        {
            if (joystickFadeLerp > 0.0f)
            {
                float inv = 1.0f / joystickFadeTime;
                joystickFadeLerp -= inv * Time.deltaTime;

                if (joystickFadeLerp < 0.0f)
                {
                    joystickFadeLerp = 0.0f;
                }

                SetJoystickAlpha(joystickFadeLerp);
            }
        }
    }

    public void SetJoystickAlpha(float alpha)
    {
        for (int i = 0; i < joystickImages.Length; i++)
        {
            RawImage image = joystickImages[i];
            image.color = new Color(image.color.r, image.color.g, image.color.b, joystickOriginalAlphas[i] * alpha);
        }
    }

    public override void UpdateTyping()
    {
        //check if keyboard was actually created
        if (mobileKeyboard == null)
        {
            username = "";
            usernameText.text = username;
            isTyping = false;

            return;
        }

        //check if the user has stopped typing
        if (mobileKeyboard.status == TouchScreenKeyboard.Status.Canceled || mobileKeyboard.status == TouchScreenKeyboard.Status.Done)
        {
            username = "";
            usernameText.text = username;
            isTyping = false;

            return;
        }

        string text = mobileKeyboard.text;

        username = "";

        //sanitise inputs
        int length = text.Length;

        for (int i = 0; i < length; i++)
        {
            username += MathExtension.SanitiseSimplifiedAlphabetChar(text[i]);
        }

        usernameText.text = username;

        if (username.Length == 3)
        {
            isTyping = false;

            client.preferences.username = username;
            client.SendPreferences(client.preferences);
        }
    }
}
