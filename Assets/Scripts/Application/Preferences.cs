using UnityEngine;

public class Preferences
{
    //current fields
    public Settings.EPlatformType platformType;
    public FixedPoint entityCount;
    public string username = "";

    //previous fields
    public string previousUsername = "";

    public Preferences()
    {
        entityCount = new FixedPoint(0.0f, 1.0f, Settings.ENTITY_LIMIT_STEP);
        entityCount.value = Settings.ENTITY_LIMIT_STEP;
    }

    public void Reset()
    {
        previousUsername = "";
    }

    public void WriteToStream(ref BitStream stream)
    {
        stream.WriteInt((int)platformType, 3);

        entityCount.WriteFixedPoint(ref stream);

        stream.WriteBool(username != previousUsername);

        if (username != previousUsername)
        {
            byte b1 = MathExtension.ConvertSimplifiedAlphabetToByte(username[0]);
            byte b2 = MathExtension.ConvertSimplifiedAlphabetToByte(username[1]);
            byte b3 = MathExtension.ConvertSimplifiedAlphabetToByte(username[2]);

            stream.WriteBits(b1, 5);
            stream.WriteBits(b2, 5);
            stream.WriteBits(b3, 5);

            //update previous field
            previousUsername = username;
        }
    }

    public void ReadFromStream(ref BitStream stream)
    {
        platformType = (Settings.EPlatformType)stream.ReadInt(3);

        entityCount.ReadFixedPoint(ref stream);

        bool hasName = stream.ReadBool();

        if (hasName)
        {
            char c1 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
            char c2 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
            char c3 = MathExtension.ConvertByteToSimplifiedAlphabet(stream.ReadBits(5)[0]);
            char[] charArray = new char[3] { c1, c2, c3 };

            username = new string(charArray);
        }
    }
}
