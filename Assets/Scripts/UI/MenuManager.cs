using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public EInput inputType;

    public bool isActive = false;

    public Client client;

    public GameObject gamePanel;
    public GameObject basePanel;
    public GameObject settingsPanel;

    //menu callbacks (for mobile)
    public ButtonData buttonData;
    public TouchData touchData;
    
    public List<GameObject> stack;

    public GameObject loading;

    //game information
    public RawImage crosshair;

    //sensitivity variables
    public float minSensitivity = 0.01f;
    public float maxSensitivity = 0.2f;
    public Slider sensitivitySlider;
    public Slider entityLimitSlider;

    //third-person variables
    public Text perspectiveButtonText;

    //window variables
    public Text windowText;
    public int previousWidth = 400;
    public int previousHeight = 300;

    //disconnect warning variables
    protected bool warning = false;
    public float warningThreshold = 3.0f;
    protected float warningFadeLerp = 0.0f;
    public float warningFadeTime = 0.0f;
    public Image[] warningImages;
    public RawImage[] warningRawImages;
    public Text warningText;
    protected float[] warningOriginalAlphas;
    protected float[] warningOriginalRawAlphas;

    //network information variables
    protected bool information = false;

    public GameObject informationPanel;
    public Text informationText1;
    public Text informationText2;

    public bool isTyping = false;
    public string username = "";
    public Text usernameText;
    public TouchScreenKeyboard mobileKeyboard;

    //audio variables
    public AudioSource soundSource;
    public Slider volumeSlider;

    void Start()
    {
        Initialise();   
    }

    public virtual void Initialise()
    {
        entityLimitSlider.value = Settings.ENTITY_LIMIT_STEP;

        warningOriginalAlphas = new float[warningImages.Length];

        for (int i = 0; i < warningImages.Length; i++)
        {
            warningOriginalAlphas[i] = warningImages[i].color.a;
        }

        warningOriginalRawAlphas = new float[warningRawImages.Length];

        for (int i = 0; i < warningRawImages.Length; i++)
        {
            warningOriginalRawAlphas[i] = warningRawImages[i].color.a;
        }

        SetWarningAlpha(0.0f);

        Add(gamePanel);
    }

    void Update()
    {
        PerFrameUpdate();
    }
    
    public virtual void PerFrameUpdate()
    {
        UpdateLoading();
        UpdateCrosshair();
        UpdateWarning();

        if (information)
        {
            UpdateInformation();
        }

        if (isTyping)
        {
            UpdateTyping();
        }
    }

    public void UpdateLoading()
    {
        if (loading == null)
        {
            return;
        }

        loading.SetActive(client.proxyId < 0);

        if (loading.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ActivateMenu()
    {
        //set initial state of option elements
        sensitivitySlider.value = Mathf.InverseLerp(minSensitivity, maxSensitivity, client.inputManager.cameraController.sensitivity);

        Add(basePanel);

        isActive = true;
        client.inputManager.menuOverride = true;
    }

    public void Pop()
    {
        stack[^1].SetActive(false);
        stack.RemoveAt(stack.Count - 1);

        if (stack.Count > 0)
        {
            stack[^1].SetActive(true);
        }
    }

    public void Add(GameObject menu)
    {
        if (stack.Count > 0)
        {
            stack[^1].SetActive(false);
        }

        stack.Add(menu);
        menu.SetActive(true);
    }

    public virtual void DisconnectButtonPressed()
    {
        
    }

    public void ResumeButtonPressed()
    {
        if (isTyping)
        {
            username = "";
            usernameText.text = username;
            isTyping = false;
        }

        Pop();

        isActive = false;
        client.inputManager.menuOverride = false;
    }

    public void MenuButtonPressed()
    {
        if (!isActive)
        {
            ActivateMenu();
        }
    }

    public void SettingsButtonPressed()
    {
        Add(settingsPanel);
    }

    public void BackButtonPressed()
    {
        if (isTyping)
        {
            username = "";
            usernameText.text = username;
            isTyping = false;
        }

        Pop();
    }

    public void WindowButtonPressed()
    {
        if (!Screen.fullScreen)
        {
            previousWidth = Screen.width;
            previousHeight = Screen.height;

            Screen.SetResolution(1920, 1080, true);
            windowText.text = "Windowed";
        }
        else
        {
            Screen.SetResolution(previousWidth, previousHeight, false);

            windowText.text = "Full-Screen";
        }
    }

    public void SensitivitySliderChanged()
    {
        //slider can be set after editor stops
        if (stack.Count > 0)
        {
            client.inputManager.cameraController.sensitivity = Mathf.Lerp(minSensitivity, maxSensitivity, sensitivitySlider.value);
        }
    }

    public void EntityLimitSliderChanged()
    {
        //slider can be set after editor stops
        if (stack.Count > 0)
        {
            client.preferences.entityCount.value = entityLimitSlider.normalizedValue;
            client.SendPreferences(client.preferences);
        }
    }

    public void PerspectiveButtonPressed()
    {
        client.inputManager.cameraController.TogglePerspective();
    }

    public void InformationButtonPressed()
    {
        information = !information;

        if (information)
        {
            informationPanel.SetActive(true);
        }
        else
        {
            informationPanel.SetActive(false);
        }
    }

    public void ChangeNamePressed()
    {
        username = "";
        usernameText.text = username;
        isTyping = true;

        if (inputType == EInput.MOBILE)
        {
            mobileKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.ASCIICapable, false, false, false);
        }
    }

    public void VolumeSliderChanged()
    {
        soundSource.volume = volumeSlider.normalizedValue;
    }

    public void UpdateCrosshair()
    {
        //crosshair.enabled = !client.inputManager.cameraController.isThirdPerson;
    }

    public void UpdateWarning()
    {
        if (client.idleTime > warningThreshold)
        {
            float timeTillTimeout = Settings.TIMEOUT - client.idleTime;

            if (timeTillTimeout < 0.0f)
            {
                timeTillTimeout = 0.0f;
            }

            timeTillTimeout = System.MathF.Round(timeTillTimeout, 1);

            if (inputType == EInput.DESKTOP)
            {
                warningText.text = "WARNING: connection problem\r\nauto-disconnect in: " + timeTillTimeout.ToString() + " seconds";
            }
            else if (inputType == EInput.MOBILE)
            {
                warningText.text = timeTillTimeout.ToString();
            }

            warning = true;
        }
        else
        {
            warning = false;
        }

        if (warning)
        {
            if (warningFadeLerp < 1.0f)
            {
                float inv = 1.0f / warningFadeTime;
                warningFadeLerp += inv * Time.deltaTime;

                if (warningFadeLerp > 1.0f)
                {
                    warningFadeLerp = 1.0f;
                }

                SetWarningAlpha(warningFadeLerp);
            }
        }
        else
        {
            if (warningFadeLerp > 0.0f)
            {
                float inv = 1.0f / warningFadeTime;
                warningFadeLerp -= inv * Time.deltaTime;

                if (warningFadeLerp < 0.0f)
                {
                    warningFadeLerp = 0.0f;
                }

                SetWarningAlpha(warningFadeLerp);
            }
        }
    }

    public void SetWarningAlpha(float alpha)
    {
        for (int i = 0; i < warningImages.Length; i++)
        {
            Image image = warningImages[i];
            image.color = new Color(image.color.r, image.color.g, image.color.b, warningOriginalAlphas[i] * alpha);
        }

        for (int i = 0; i < warningRawImages.Length; i++)
        {
            RawImage image = warningRawImages[i];
            image.color = new Color(image.color.r, image.color.g, image.color.b, warningOriginalRawAlphas[i] * alpha);
        }

        warningText.color = new Color(warningText.color.r, warningText.color.g, warningText.color.b, alpha);
    }

    public void UpdateInformation()
    {
        string connectString = "OFFLINE";

        if (client.proxyId >= 0)
        {
            connectString = "ONLINE";
        }

        int fps = (int)client.statistics.fpsAverage.GetAverage();
        int ping = (int)client.statistics.pingAverage.GetAverage();
        int entityCount = client.entityManager.entities.Count;
        int playerCount = client.players.Count;

        informationText1.text = connectString + "\nFPS " + 
                                fps.ToString() + "\nPING " + 
                                ping.ToString() + "\nPLAYERS " +
                                playerCount.ToString() + "\nENTITIES " + 
                                entityCount.ToString();

        float sent = (float)(client.statistics.sentBytes * 8);
        string sentUnit = "bps";

        if (sent >= 1000.0f)
        {
            sent /= 1000.0f;
            sentUnit = "kbps";

            if (sent >= 1000.0f)
            {
                sent /= 1000.0f;
                sentUnit = "mbps";
            }
        }

        sent = System.MathF.Round(sent, 1);

        float recieved = (float)(client.statistics.recievedBytes * 8);
        string recievedUnit = "bps";

        if (recieved >= 1000.0f)
        {
            recieved /= 1000.0f;
            recievedUnit = "kbps";

            if (recieved >= 1000.0f)
            {
                recieved /= 1000.0f;
                recievedUnit = "mbps";
            }
        }

        recieved = System.MathF.Round(recieved, 1);

        informationText2.text = "UP " + sent.ToString() + sentUnit + "\nDOWN " + 
                                recieved.ToString() + recievedUnit;
    }

    public virtual void UpdateTyping()
    {

    }
}
