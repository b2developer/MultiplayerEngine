using System;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class ButtonData
{
    public bool jump = false;
    public bool fire = false;
    public Vector2 joystick = Vector2.zero;
}

public class ManagedTouch
{
    public int index = 0;
    public int fingerId = 0;

    public Touch touch;
    public Vector2 start;
}

public class TouchData
{
    public List<ManagedTouch> touchList;

    public TouchData()
    {
        touchList = new List<ManagedTouch>();
    }
}

public class MobileInputManager : InputManager
{
    public MobileInputSample sample;

    public ButtonData buttonData;
    public TouchData touchData;

    public Vector2 touchAnchor = Vector2.zero;
    public bool isTouching = false;

    public override void Initialise()
    {
        sample = new MobileInputSample();
        sample.Initialise();

        buttonData = new ButtonData();
        touchData = new TouchData();

        cameraController.inputType = EInput.MOBILE;
        cameraController.menuManager.inputType = EInput.MOBILE;

        cameraController.touchData = touchData;

        cameraController.menuManager.buttonData = buttonData;
        cameraController.menuManager.touchData = touchData;
    }

    public override InputSample GetInputSample()
    {
        return sample;
    }

    public override void PerFrameUpdate()
    {
        MobileMenuManager mobileMenuManager = cameraController.menuManager as MobileMenuManager;

        PollTouches();
        mobileMenuManager.PollVirtualJoystick();

        sample.joystickX.value = -buttonData.joystick.x;
        sample.joystickY.value = buttonData.joystick.y;

        sample.joystickX.Quantize();
        sample.joystickY.Quantize();

        mobileMenuManager.PollGameButtons();

        //sort button data
        if (buttonData.jump)
        {
            sample.jump.state = EButtonState.ON_PRESS;
            sample.jump.changeDetected = true;

            buttonData.jump = false;
        }

        if (buttonData.fire)
        {
            sample.fire.state = EButtonState.ON_PRESS;
            sample.fire.changeDetected = true;

            buttonData.fire = false;
        }

        //inputs must be sampled every frame
        sample.jump.Poll(true, fuzz);
        sample.fire.Poll(true, fuzz);

        cameraController.Poll();
        sample.yaw = cameraController.yaw;

        sample.pitch = 0.0f;

        if (!cameraController.isThirdPerson || cameraController.isRouted)
        {
            sample.pitch = cameraController.pitch;
        }
    }

    public override void Tick()
    {
        sample.jump.Reset();
        sample.fire.Reset();

        sample.timestamp++;

        if (sample.timestamp >= Settings.MAX_INPUT_INDEX)
        {
            sample.timestamp -= Settings.MAX_INPUT_INDEX;
        }
    }

    public void PollTouches()
    {
        int touchListCount = touchData.touchList.Count;

        //remove out of order touch data objects
        for (int i = 0; i < touchListCount; i++)
        {
            ManagedTouch item = touchData.touchList[i];

            bool missing = true;

            for (int j = 0; j < Input.touchCount; j++)
            {
                Touch touch = Input.GetTouch(j);

                if (touch.fingerId == item.fingerId)
                {
                    item.touch = touch;
                    missing = false;
                }

                if (item.touch.phase == TouchPhase.Canceled || item.touch.phase == TouchPhase.Ended)
                {
                    touchData.touchList.RemoveAt(i);
                    i--;
                    touchListCount--;
                    break;
                }
            }

            //this should never trigger but it's a good fail safe
            if (missing)
            {
                touchData.touchList.RemoveAt(i);
                i--;
                touchListCount--;
            }
        }

        //introduce new touch data objects
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            if (touch.phase != TouchPhase.Began)
            {
                continue;
            }

            //absorbed by ui
            if (MobileInputManager.IsTouchOverUI(touch))
            {
                continue;
            }

            bool missing = true;

            for (int j = 0; j < touchListCount; j++)
            {
                ManagedTouch item = touchData.touchList[j];

                if (item.fingerId == touch.fingerId)
                {
                    missing = false;
                    break;
                }
            }

            if (missing)
            {
                ManagedTouch newItem = new ManagedTouch();

                newItem.index = i;
                newItem.fingerId = touch.fingerId;
                newItem.touch = touch;
                newItem.start = touch.position;

                touchData.touchList.Add(newItem);
            }
        }
    }

    public static bool IsTouchOverUI(Touch touch)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(touch.position.x, touch.position.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0;
    }

    public static bool IsTouchOverButton(Touch touch, UnityEngine.UI.Button button)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(touch.position.x, touch.position.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        int count = results.Count;

        //check results for desired button
        for (int i = 0; i < count; i++)
        {
            RaycastResult result = results[i];

            if (result.gameObject == button.gameObject)
            {
                return true;
            }
        }

        return false;
    }
}
