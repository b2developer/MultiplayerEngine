using UnityEngine;

public class PlayerCosmetics : MonoBehaviour
{
    public MeshRenderer leftBranding;
    public MeshRenderer rightBranding;

    public Material[] brandings;

    public Settings.EPlatformType previousBranding = Settings.EPlatformType.UNDEFINED;
    public Settings.EPlatformType branding = Settings.EPlatformType.UNDEFINED;

    public void WriteToStream(ref BitStream stream)
    {
        stream.WriteInt((int)branding, 3);
    }

    public void ReadFromStream(ref BitStream stream)
    {
        Settings.EPlatformType newBranding = (Settings.EPlatformType)stream.ReadInt(3);

        if (branding != newBranding)
        {
            branding = newBranding;
            SetBranding(branding);
        }
    }

    public int GetBitLength()
    {
        return 3;
    }

    public void WriteToStreamPartial(ref BitStream stream, int dirtyFlag)
    {
        if ((dirtyFlag & (int)EPlayerProperties.BRANDING) > 0)
        {
            stream.WriteInt((int)branding, 3);
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
    }
    
    public int GetBitLengthPartial(int dirtyFlag)
    {
        int total = 0;

        if ((dirtyFlag & (int)EPlayerProperties.BRANDING) > 0)
        {
            total += 3;
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
}
