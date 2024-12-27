using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Settings
{
    public enum EBuildType
    {
        SERVER,
        CLIENT,
    }

    public enum EPlatformType
    {
        UNDEFINED = 0,
        WINDOWS = 1,
        MAC = 2,
        LINUX = 3,
        ANDROID = 4,
        IOS = 5,
        VR = 6,
        BOT = 7,
    }

    public enum EMessageType
    {
        UNRELIABLE = 0, //udp with header
        RELIABLE_UNORDERED = 1, //udp with acknowledgements
        RELIABLE_ACK = 2, //acknowledgement bitfield
        UNRELIABLE_ORDERED = 3, //udp but ignores out of order
        RAW = 4, //no additional headers or calculations are made
    }

    public enum ELinkingStateType
    {
        HOST = 0, //register server to the network state
        CONNECT = 1, //register client information
        SUCCESS = 2,
        FAIL = 3,
        LINK = 4, //incoming network data of other end
        LOCAL_IP_DETECTED = 5, //soft error, public ip needed
    }

    public enum Endian
    {
        LITTLE = 0,
        BIG = 1,
    }

    //signs buffers and prevents older clients from joining
    public const int VERSION = 5;

    public static EPlatformType platformType;

    //buffer settings
    public const int BUFFER_LIMIT = 900;
    public const int BUFFER_OVERFLOW = 5;
    public const int BUFFER_SIZE = 1300;
    public const int UDP_OVERHEAD = 28; //28 bytes in empty udp packets

    //platform specific settings
    public const Endian STREAM_ENDIANESS = Endian.LITTLE; //most modern computers are mostly little
    public static Endian PLATFORM_ENDIANNESS = Endian.LITTLE; //setting to little endianness saves the most endian swaps

    //reliable UDP settings
    public const int MAX_ACK_NUMBER = 16384;
    public const float RELIABLE_TIMEOUT = 1.0f;
    public const int ACK_FIELD_SIZE = 32;

    //ordered UDP settings
    public const int MAX_INDEX_NUMBER = 64;

    //connection settings
    public const float CLIENT_LINKER_TIMEOUT = 1.0f;
    public const float SERVER_LINKER_TIMEOUT = 0.1f;
    public const float TIMEOUT = 10.0f;

    //partials can result in inaccuracies due to packet loss, a full packet is needed every now and then
    public const int PARTIAL_TO_FULL_RATIO = 20;

    //index settings  
    public const int MAX_ENTITY_INDEX = 65536;
    public const int MAX_INPUT_INDEX = 65536;
    public const int MAX_TICKET_INDEX = 256;

    public const int MINIMUM_ENTITY_LIMIT = 8;
    public const float ENTITY_LIMIT_STEP = 0.05f;

    public static int MAX_ENTITY_BITS = 0;
    public static int MAX_TYPE_BITS = 0;
    public static int MAX_FUNCTION_TYPE_BITS = 0;
    public static int MAX_TICKET_BITS = 0;

    //fixed point settings
    public const float WORLD_MIN_X = -250.0f;
    public const float WORLD_MIN_Y = -100.0f;
    public const float WORLD_MIN_Z = -250.0f;
    public const float WORLD_MAX_X = 250.0f;
    public const float WORLD_MAX_Y = 100.0f;
    public const float WORLD_MAX_Z = 250.0f;

    //overflow settings
    public const int MAX_MESSAGE_QUEUE_SIZE = 8;
    public const int MAX_CLIENT_SIDE_PREDICTION_SIZE = 40;

    //client smoothing settings
    public const float INTERPOLATION_PERIOD = 0.07f;
    public const float MAX_INTERPOLATION_DISTANCE = 6.0f;

    //timeout limit for entities that aren't updated
    public const float ENTITY_TIMEOUT_TIME = 2.0f;

    public static void Initialise(EBuildType buildType, ref EPlatformType platformType)
    {
        Settings.platformType = platformType;

        MAX_ENTITY_BITS = MathExtension.RequiredBits(MAX_ENTITY_INDEX);
        MAX_TYPE_BITS = MathExtension.RequiredBits((int)EObject.MAX);
        MAX_FUNCTION_TYPE_BITS = MathExtension.RequiredBits((int)EFunction.MAX);
        MAX_TICKET_BITS = MathExtension.RequiredBits(MAX_TICKET_INDEX);

        if (!System.BitConverter.IsLittleEndian)
        {
            PLATFORM_ENDIANNESS = Endian.BIG;
        }

        if (buildType == EBuildType.SERVER)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 300;

            return;
        }

        if (platformType == EPlatformType.WINDOWS)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 300;
        }
        else if (platformType == EPlatformType.MAC)
        {
            //most macs are laptops, save battery

            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
        }
        else if (platformType == EPlatformType.LINUX)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
        }
        else if (platformType == EPlatformType.ANDROID)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
        }
        else if (platformType == EPlatformType.IOS)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
        }
        else if (platformType == EPlatformType.VR)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 300;
        }
        else if (platformType == EPlatformType.BOT)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 20;

            platformType = EPlatformType.WINDOWS;
            Settings.platformType = EPlatformType.WINDOWS;
        }
    }
}
