using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class DesktopMenuManager : MenuManager
{
    public override void Initialise()
    {
        base.Initialise();

        if (Screen.fullScreen)
        {
            windowText.text = "Windowed";
        }
        else
        {
            windowText.text = "Full-Screen";
        }
    }

    public override void PerFrameUpdate()
    {
        base.PerFrameUpdate();

        //toggle perspective hotkey
        if (!isActive && Input.GetKeyDown(KeyCode.T))
        {
            client.inputManager.cameraController.TogglePerspective();
        }
    }

    public override void DisconnectButtonPressed()
    {
        client.SendDisconnect();

        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public override void UpdateTyping()
    {
        for (int i = 97; i <= 122; i++)
        {
            KeyCode keycode = (KeyCode)i;

            if (Input.GetKeyDown(keycode))
            {
                byte byteKey = (byte)(i - 97);
                char typed = MathExtension.ConvertByteToSimplifiedAlphabet(byteKey);

                username += typed;
                usernameText.text = username;

                if (username.Length == 3)
                {
                    isTyping = false;
                    client.preferences.username = username;
                    client.SendPreferences(client.preferences);
                }
            }
        }
    }
}
