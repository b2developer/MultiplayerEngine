using System.Collections.Generic;
using System.Net;
using UnityEngine;

public enum EClientPacket
{
    HELLO = 0, //connection handshake
    PING = 1, //keeps the connection alive
    PREFERENCES = 2, //local settings for server
    INPUT = 3, //send input
    DISCONNECT = 4, //break connection
}

public enum EReplicationDestination
{
    UNDIRECTED = 0,
    TO_MIDDLE = 1,
    TO_CLIENT = 2,
}

public enum EServerPacket
{
    HELLO = 0, //connection handshake
    PING = 1, //keep connection alive
    REPLICATION = 2, //world snapshot data
    PROXY = 3, //snapshot data of player, doesn't apply any lossy compression
}

public class Client : MonoBehaviour
{
    public Settings.EPlatformType platformType;

    public int proxyId = -1;
    public PlayerEntity proxy = null;

    public List<RecievedMessage> messageBuffer;

    public SimpleEntropyEncoder entropyEncoder;

    public float idleTime = 0.0f;

    public Channel channel;
    public ClientLinker linker;

    public int bindingPort = 5005;
    private int baseBindingPort = 0;

    public string linkerAddress = "1.157.72.236";
    public int linkerPort = 7000;

    public Preferences preferences;
    public UpdateManager updateManager;
    public ObjectRegistry objectRegistry;
    public EntityManager entityManager;
    public InputManager inputManager;
    public ClientSidePrediction clientSidePrediction;
    public Statistics statistics;

    public List<PlayerEntity> players;

    public bool isConfirmed = false;

    void Start()
    {
        Settings.Initialise(Settings.EBuildType.CLIENT, ref platformType);

        //this is nonsense, but useful nonsense --------------------------------------------
        baseBindingPort = bindingPort;
        bindingPort = baseBindingPort - Random.Range(0, 5000);

        IPAddress linkerIp = IPOperation.ResolveDomainName(linkerAddress);

        linker = new ClientLinker(LinkingCompleted, linkerIp, linkerPort, bindingPort);
        linker.Initialise();
        linker.SendConnectClientRequest();

        channel = new Channel();
        channel.dynamicPort = true;
        channel.sendMessageCallback = StoreRecievedMessage;
        channel.sentBytesCallback = statistics.OnBytesSent;
        channel.recievedBytesCallback = statistics.OnBytesRecieved;

        messageBuffer = new List<RecievedMessage>();

        entropyEncoder = new SimpleEntropyEncoder();

        preferences = new Preferences();
        preferences.platformType = platformType;

        Physics.simulationMode = SimulationMode.Script;

        updateManager.Initialise();
        UpdateManager.instance.managerFunction += Tick;

        entityManager.Initialise();

        inputManager.Initialise();

        players = new List<PlayerEntity>();
        objectRegistry.spawnPlayerCallback += OnPlayerSpawned;
        objectRegistry.destroyPlayerCallback += OnPlayerDestroyed;
    }

    //this is also "OnConnect()"
    public void LinkingCompleted(int bindingPort, IPAddress remoteAddress, int remotePort)
    {
        channel.Bind(new IPEndPoint(IPAddress.Any, bindingPort));
        channel.Connect(new IPEndPoint(remoteAddress, remotePort));

        idleTime = 0.0f;

        preferences.Reset();

        //start the recursive loop
        channel.Recieve();
    }

    public void UpdateTimeout(float deltaTime)
    {
        idleTime += deltaTime;

        if (idleTime >= Settings.TIMEOUT)
        {
            channel.Dump();
            channel = new Channel();
            channel.sendMessageCallback = StoreRecievedMessage;
            channel.sentBytesCallback = statistics.OnBytesSent;
            channel.recievedBytesCallback = statistics.OnBytesRecieved;

            bindingPort = baseBindingPort - Random.Range(0, 5000);

            //clear states
            messageBuffer = new List<RecievedMessage>();
            idleTime = 0.0f;

            //gracefully remove the animator since the entity manager can't
            if (proxy != null)
            {
                objectRegistry.RemovePlayerAnimator(proxy);
            }

            entityManager.Dump();

            proxyId = -1;
            proxy = null;
            
            inputManager.cameraController.focus = null;

            clientSidePrediction.unacknowledgedInputs = new List<InputSample>();
            clientSidePrediction.proxy = null;

            isConfirmed = false;

            //reset
            inputManager.Initialise();

            IPAddress linkerIp = IPOperation.ResolveDomainName(linkerAddress);

            linker.Reset(linkerIp, linkerPort, bindingPort);
            linker.Initialise();
            linker.SendConnectClientRequest();
        }
    }

    public void FindProxy()
    {
        if (proxy != null)
        {
            return;
        }

        if (proxyId >= 0)
        {
            Entity entity = entityManager.GetEntityFromId((uint)proxyId);

            if (entity != null)
            {
                proxy = entity as PlayerEntity;

                proxy.isLocal = true;
                proxy.accept = false;
                proxy.body.isKinematic = false;

                objectRegistry.AddPlayerAnimator(proxy);

                inputManager.cameraController.focus = proxy.animator.transform;

                if (inputManager.cameraController.isThirdPerson)
                {
                    inputManager.cameraController.TogglePerspective();
                }

                clientSidePrediction.proxy = proxy;
            }
        }
    }

    public void OnPlayerSpawned(Entity entity)
    {
        PlayerEntity player = entity as PlayerEntity;
        players.Add(player);
    }

    public void OnPlayerDestroyed(Entity entity)
    {
        PlayerEntity player = entity as PlayerEntity;
        players.Remove(player);
    }

    void Update()
    {
        if (linker.state != ClientLinker.ELinkerState.READY)
        {
            linker.Update(Time.deltaTime);
            return;
        }

        UpdateTimeout(Time.deltaTime);

        inputManager.PerFrameUpdate();

        if (proxy != null)
        {
            InputSample sample = inputManager.GetInputSample();
            proxy.animator.transform.rotation = Quaternion.LookRotation(-sample.GetLookVector());
        }
    }

    private void LateUpdate()
    {
        if (proxy != null)
        {
            //set rotation of nametags
            foreach (PlayerEntity player in players)
            {
                Vector3 relative = inputManager.cameraController.transform.position - player.nameTag.transform.position;

                //looks funny but it works
                player.nameTag.transform.position = player.transform.position + player.nameTagLocalPosition;
                player.nameTag.transform.rotation = Quaternion.LookRotation(relative, inputManager.cameraController.transform.up);
            }
        }
    }

    public void Tick()
    {
        if (linker.state != ClientLinker.ELinkerState.READY)
        {
            return;
        }

        FindProxy();

        //server reconciliation happens here
        ProcessNetworkMessages();

        if (isConfirmed)
        {
            SendInput();

            PreUpdateProxy();

            Physics.Simulate(Time.fixedDeltaTime);
            Physics.SyncTransforms();

            PostUpdateProxy();

            InputSample sample = inputManager.GetInputSample();
            clientSidePrediction.StoreInput(sample);
        }
        else
        {
            SendHello();
        }

        inputManager.Tick();

        foreach (Entity entity in entityManager.entities)
        {
            bool isActive = !entity.TickTimeout();
            entity.SetActive(isActive);
        }

        channel.ResendReliableMessages(Time.fixedDeltaTime);
    }

    public byte[] CompressData(ref byte[] buffer, int bytesWritten)
    {
        //compress buffer
        int maxCompressionLength = entropyEncoder.MaxCompressionSize(bytesWritten);

        BitStream compressedStream = new BitStream(maxCompressionLength);
        entropyEncoder.WriteCompressedBytes(ref compressedStream, buffer);

        //copy compressed buffer into final byte array
        int compressedBytesWritten = (int)System.MathF.Ceiling(compressedStream.bitIndex / 8.0f);

        byte[] compressedBuffer = new byte[compressedBytesWritten];
        System.Buffer.BlockCopy(buffer, 0, compressedStream.buffer, 0, compressedBytesWritten);

        return compressedBuffer;
    }

    public byte[] DecompressData(ref BitStream stream)
    {
        byte[] decompressedBuffer = entropyEncoder.ReadCompressedBytes(ref stream);
        return decompressedBuffer;
    }

    public void SendHello()
    {
        BitStream stream = new BitStream(1);
        stream.WriteInt((int)EClientPacket.HELLO, 3);

        stream.WriteInt((int)platformType, 3);

        channel.Send(stream.buffer, Settings.EMessageType.UNRELIABLE);
    }

    public void SendPing()
    {
        BitStream stream = new BitStream(1);
        stream.WriteInt((int)EClientPacket.PING, 3);

        channel.Send(stream.buffer, Settings.EMessageType.UNRELIABLE);
    }

    public void SendPreferences(Preferences preferences)
    {
        BitStream stream = new BitStream(8);
        stream.WriteInt((int)EClientPacket.PREFERENCES, 3);

        preferences.WriteToStream(ref stream);
        channel.Send(stream.buffer, Settings.EMessageType.RELIABLE_UNORDERED);
    }

    public void SendInput()
    {
        BitStream stream = new BitStream(17);
        stream.WriteInt((int)EClientPacket.INPUT, 3);

        InputSample sample = inputManager.GetInputSample();
        sample.WriteToStream(ref stream);

        channel.Send(stream.buffer, Settings.EMessageType.UNRELIABLE);
    }

    public void SendDisconnect()
    {
        BitStream stream = new BitStream(1);
        stream.WriteInt((int)EClientPacket.DISCONNECT, 3);

        channel.Send(stream.buffer, Settings.EMessageType.RELIABLE_UNORDERED);
    }

    public void PreUpdateProxy()
    {
        if (proxy != null)
        {
            InputSample sample = inputManager.GetInputSample();

            proxy.input = sample.Clone();
            proxy.ManualTick();

            proxy.animator.interpolationFilter.SetPreviousState(proxy.animator.transform.position, proxy.animator.transform.rotation);
        }
    }

    public void PostUpdateProxy()
    {
        if (proxy != null)
        {
            proxy.animator.interpolationFilter.SetCurrentState(proxy.body.position, proxy.body.rotation);
        }
    }

    public void Recieve()
    {
        if (channel.isBusy)
        {
            return;
        }

        channel.Recieve();
    }

    public void ProcessNetworkMessages()
    {
        lock (messageBuffer)
        {
            int messageCount = messageBuffer.Count;

            if (messageCount > 0)
            {
                idleTime = 0.0f;
            }

            foreach (RecievedMessage message in messageBuffer)
            {
                if (message.type == Settings.EMessageType.UNRELIABLE)
                {
                    //decompress data
                    BitStream compressedStream = new BitStream(message.buffer);
                    byte[] decompressedData = DecompressData(ref compressedStream);
                    BitStream stream = new BitStream(decompressedData);

                    byte packetByte = stream.ReadBits(3)[0];

                    EServerPacket packetType = (EServerPacket)packetByte;

                    if (packetType == EServerPacket.PING)
                    {

                    }
                }
                else if (message.type == Settings.EMessageType.RELIABLE_UNORDERED)
                {
                    //decompress data
                    BitStream compressedStream = new BitStream(message.buffer);
                    byte[] decompressedData = DecompressData(ref compressedStream);
                    BitStream stream = new BitStream(decompressedData);

                    byte packetByte = stream.ReadBits(3)[0];

                    EServerPacket packetType = (EServerPacket)packetByte;

                    if (packetType == EServerPacket.HELLO)
                    {
                        isConfirmed = true;
                        proxyId = (int)stream.ReadInt(Settings.MAX_ENTITY_BITS);

                        FindProxy();

                        //confirmation of player is complete, send the details over
                        SendPreferences(preferences);
                    }
                    else if (packetType == EServerPacket.REPLICATION)
                    {
                        EReplicationDestination dummyDestination = (EReplicationDestination)stream.ReadInt(2);

                        if (dummyDestination == EReplicationDestination.TO_CLIENT)
                        {
                            int dummyTicket = stream.ReadInt(Settings.MAX_TICKET_BITS);
                        }

                        entityManager.ReadReplicationData(ref stream);
                    }
                }
                else if (message.type == Settings.EMessageType.UNRELIABLE_ORDERED)
                {
                    //decompress data
                    BitStream compressedStream = new BitStream(message.buffer);
                    byte[] decompressedData = DecompressData(ref compressedStream);
                    BitStream stream = new BitStream(decompressedData);

                    byte packetByte = stream.ReadBits(3)[0];

                    EServerPacket packetType = (EServerPacket)packetByte;

                    if (packetType == EServerPacket.REPLICATION)
                    {
                        entityManager.ReadReplicationData(ref stream);
                    }
                    else if (packetType == EServerPacket.PROXY)
                    {
                        int dummyTicket = stream.ReadInt(Settings.MAX_TICKET_BITS);

                        int timestamp = -1;
                        bool hasTimestamp = stream.ReadBool();

                        if (hasTimestamp)
                        {
                            timestamp = (int)stream.ReadInt(16);

                            InputSample sample = inputManager.GetInputSample();
                            int timestampDifference = MathExtension.DiffWrapped(timestamp, sample.timestamp, Settings.MAX_INPUT_INDEX);

                            float estimatedTime = timestampDifference * Time.fixedDeltaTime;
                            statistics.OnPingRecieved(estimatedTime);
                        }

                        FindProxy();

                        if (proxy != null)
                        {
                            if (hasTimestamp)
                            {
                                proxy.ReadPlayerFromStream(ref stream);

                                //client side predictor simulates all inputs after timestamp
                                clientSidePrediction.ReconcileWithServer(timestamp);
                            }
                        }
                    }
                }
            }

            messageBuffer.Clear();
        }
    }

    public void StoreRecievedMessage(RecievedMessage message)
    {
        lock (messageBuffer)
        {
            messageBuffer.Add(message);

            int count = messageBuffer.Count;

            if (count > Settings.MAX_MESSAGE_QUEUE_SIZE)
            {
                //find the earliest unreliable data
                for (int i = 0; i < count; i++)
                {
                    if (messageBuffer[i].type == Settings.EMessageType.UNRELIABLE)
                    {
                        messageBuffer.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}