using Unity.VisualScripting;
using UnityEngine;

public class PlayerCosmetics : MonoBehaviour
{
    public MeshRenderer leftBranding;
    public MeshRenderer rightBranding;

    public Material[] brandings;

    public Settings.EPlatformType previousBranding = Settings.EPlatformType.UNDEFINED;
    public Settings.EPlatformType branding = Settings.EPlatformType.UNDEFINED;

    public MeshRenderer body;

    public Material[] bodyColours;
    public Material gold;
    public GameObject[] bodyCostumes;

    public int previousRace = 0;
    public int race = 0;

    public bool previousIsGold = false;
    public bool isGold = false;

    public void WriteToStream(ref BitStream stream)
    {
        stream.WriteInt((int)branding, 3);
        stream.WriteInt(race, 4);
        stream.WriteBool(isGold);
    }

    public void ReadFromStream(ref BitStream stream)
    {
        Settings.EPlatformType newBranding = (Settings.EPlatformType)stream.ReadInt(3);

        if (branding != newBranding)
        {
            branding = newBranding;
            SetBranding(branding);
        }

        int newRace = stream.ReadInt(4);

        if (race != newRace)
        {
            race = newRace;
            SetRace(race);
        }

        bool newIsGold = stream.ReadBool();

        if (isGold != newIsGold)
        {
            isGold = newIsGold;
            SetGold(isGold);
        }
    }

    public int GetBitLength()
    {
        return 8;
    }

    public void WriteToStreamPartial(ref BitStream stream, int dirtyFlag)
    {
        if ((dirtyFlag & (int)EPlayerProperties.BRANDING) > 0)
        {
            stream.WriteInt((int)branding, 3);
        }

        if ((dirtyFlag & (int)EPlayerProperties.RACE) > 0)
        {
            stream.WriteInt((int)race, 4);
        }

        if ((dirtyFlag & (int)EPlayerProperties.GOLD) > 0)
        {
            stream.WriteBool(isGold);
        }
    }

    public void ReadFromStreamPartial(ref BitStream stream, int dirtyFlag)
    {
        if ((dirtyFlag & (int)EPlayerProperties.BRANDING) > 0)
        {
            Settings.EPlatformType newBranding = (Settings.EPlatformType)stream.ReadInt(3);

            if (branding != newBranding)
            {
                branding = newBranding;
                SetBranding(branding);
            }
        }

        if ((dirtyFlag & (int)EPlayerProperties.RACE) > 0)
        {
            int newRace = stream.ReadInt(4);

            if (race != newRace)
            {
                race = newRace;
                SetRace(race);
            }
        }

        if ((dirtyFlag & (int)EPlayerProperties.GOLD) > 0)
        {
            bool newIsGold = stream.ReadBool();

            if (isGold != newIsGold)
            {
                isGold = newIsGold;
                SetGold(isGold);
            }
        }
    }
    
    public int GetBitLengthPartial(int dirtyFlag)
    {
        int total = 0;

        if ((dirtyFlag & (int)EPlayerProperties.BRANDING) > 0)
        {
            total += 3;
        }

        if ((dirtyFlag & (int)EPlayerProperties.RACE) > 0)
        {
            total += 4;
        }

        if ((dirtyFlag & (int)EPlayerProperties.GOLD) > 0)
        {
            total += 1;
        }

        return total;
    }

    public void SetBranding(Settings.EPlatformType branding)
    {
        if (branding == Settings.EPlatformType.UNDEFINED)
        {
            return;
        }

        leftBranding.material = brandings[(int)branding - 1];
        rightBranding.material = brandings[(int)branding - 1];
    }

    public void SetRace(int _race)
    {
        for (int i = 0; i < bodyCostumes.Length; i++)
        {
            bodyCostumes[i].SetActive(false);
        }

        if (!isGold)
        {
            body.material = bodyColours[_race];
        }

        bodyCostumes[_race].SetActive(true);
    }

    public void SetGold(bool _state)
    {
        if (isGold)
        {
            body.material = gold;
        }
    }
}
