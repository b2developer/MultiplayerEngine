using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;

public class MiddleClientInfo
{
    public int ticket = -1;
    public int proxyId = -1;

    public Channel channel;

    public Preferences preferences;
    public PlayerEntity proxy;

    public float idleTime = 0.0f;

    public MiddleClientInfo()
    {
        preferences = new Preferences();
    }
}

public class Middle : MonoBehaviour
{
    //global message buffer is stored outside of client info for easy locking
    public List<RecievedMessage> clientMessageBuffer;
    public List<ConcurrentQueue<RecievedMessage>> serverMessageBuffer;

    public SimpleEntropyEncoder entropyEncoder;

    //client info
    //----------
    public float idleTime = 0.0f;

    public Channel channel;
    public ClientLinker clientLinker;

    public int clientBindingPort = 5005;
    private int clientBaseBindingPort = 0;
    //----------

    //server info
    //----------
    public List<MiddleClientInfo> clients;
    public Bucket tickets;
    public ServerLinker serverLinker;

    public int serverListeningPort = 5000;
    public int serverBindingPortStart = 5001;
    public int serverBindingPortEnd = 5008;
    //----------

    public string linkerAddress = "1.157.72.236";
    public int linkerPort = 7000;

    public UpdateManager updateManager;
    public ObjectRegistry objectRegistry;
    public EntityManager entityManager;

    public int localTick = 0;

    public MovingAverage performanceAverage;
    public float performanceTimer = 0.0f;

    void Start()
    {
        Settings.EPlatformType platformType = Settings.EPlatformType.WINDOWS;
        Settings.Initialise(Settings.EBuildType.SERVER, ref platformType);

        //this is nonsense, but useful nonsense --------------------------------------------
        clientBaseBindingPort = clientBindingPort;
        clientBindingPort = clientBaseBindingPort - Random.Range(0, 5000);

        IPAddress linkerIp = IPOperation.ResolveDomainName(linkerAddress);

        //client initialisation
        //----------
        clientLinker = new ClientLinker(LinkingCompletedOnClient, linkerIp, linkerPort, clientBindingPort);
        clientLinker.isMiddle = true;
        clientLinker.Initialise();
        clientLinker.SendConnectClientRequest();

        channel = new Channel();
        channel.dynamicPort = true;
        channel.sendMessageCallback = StoreRecievedMessageOnClient;

        clientMessageBuffer = new List<RecievedMessage>();
        //----------

        //server initialisation
        //----------
        serverLinker = new ServerLinker(LinkingCompletedOnServer, linkerIp, linkerPort, serverListeningPort);
        serverLinker.isMiddle = true;
        serverLinker.Initialise();

        serverMessageBuffer = new List<ConcurrentQueue<RecievedMessage>>();

        clients = new List<MiddleClientInfo>();
        tickets = new Bucket(Settings.MAX_TICKET_INDEX);
        //----------

        entropyEncoder = new SimpleEntropyEncoder();

        Physics.simulationMode = SimulationMode.Script;

        objectRegistry.Initialise();

        updateManager.Initialise();
        UpdateManager.instance.managerFunction += Tick;

        entityManager.Initialise();

        performanceAverage = new MovingAverage(50);
    }

    public int GetNextAvailablePort()
    {
        int count = clients.Count;

        for (int port = serverBindingPortStart; port <= serverBindingPortEnd; port++)
        {
            bool free = true;

            for (int i = 0; i < count; i++)
            {
                IPEndPoint ipEnd = (IPEndPoint)clients[i].channel.socket.LocalEndPoint;

                if (port == ipEnd.Port)
                {
                    free = false;
                    break;
                }
            }

            if (free)
            {
                return port;
            }
        }

        return -1;
    }

    public MiddleClientInfo GetClientWithTicket(int ticket, out int index)
    {
        int count = clients.Count;

        for (int i = 0; i < count; i++)
        {
            MiddleClientInfo client = clients[i];

            if (client.ticket == ticket)
            {
                index = i;
                return client;
            }
        }

        index = -1;
        return null;
    }

    public MiddleClientInfo GetClientWithProxyId(int proxyId, out int index)
    {
        int count = clients.Count;

        for (int i = 0; i < count; i++)
        {
            MiddleClientInfo client = clients[i];

            if (client.proxyId == proxyId)
            {
                index = i;
                return client;
            }
        }

        index = -1;
        return null;
    }

    //this is also "OnConnect()"
    public void LinkingCompletedOnClient(int bindingPort, IPAddress remoteAddress, int remotePort)
    {
        channel.Bind(new IPEndPoint(IPAddress.Any, bindingPort));
        channel.Connect(new IPEndPoint(remoteAddress, remotePort));

        idleTime = 0.0f;

        //start the recursive loop
        channel.Recieve();

        serverLinker.SendHostRequest();
    }

    //this is also "OnConnect()"
    public void LinkingCompletedOnServer(int bindingPort, IPAddress remoteAddress, int remotePort)
    {
        Channel channel = new Channel();
        channel.dynamicPort = true;
        channel.sendMessageCallback = StoreRecievedMessageOnServer;

        MiddleClientInfo info = new MiddleClientInfo();
        info.channel = channel;

        int ticket = (int)tickets.GetFreeIndex();
        info.ticket = ticket;

        //add new local buffer inside global buffer
        serverMessageBuffer.Add(new ConcurrentQueue<RecievedMessage>());

        clients.Add(info);

        channel.Bind(new IPEndPoint(IPAddress.Any, bindingPort));
        channel.Connect(new IPEndPoint(remoteAddress, remotePort));

        int clientIndex = clients.Count - 1;

        //start the recursive loop
        channel.Recieve();

        int nextPort = GetNextAvailablePort();

        if (nextPort >= 0)
        {
            IPAddress linkerIp = IPOperation.ResolveDomainName(linkerAddress);

            serverLinker.Reset(linkerIp, linkerPort, nextPort);
            serverLinker.Initialise();
            serverLinker.SendHostRequest();
        }
    }

    public void HandleDisconnectOnServer(int index)
    {
        MiddleClientInfo client = clients[index];

        tickets.ReturnIndex((uint)client.ticket);

        IPEndPoint ipEnd = (IPEndPoint)client.channel.socket.LocalEndPoint;
        int freedPort = ipEnd.Port;

        client.channel.Dump();

        //remove client references
        serverMessageBuffer.RemoveAt(index);
        clients.RemoveAt(index);

        //restart linker if neccessary
        if (serverLinker.state == ServerLinker.ELinkerState.READY)
        {
            IPAddress linkerIp = IPOperation.ResolveDomainName(linkerAddress);

            serverLinker.Reset(linkerIp, linkerPort, freedPort);
            serverLinker.Initialise();
            serverLinker.SendHostRequest();
        }
    }

    void Update()
    {
        if (clientLinker.state != ClientLinker.ELinkerState.READY)
        {
            clientLinker.Update(Time.deltaTime);
        }

        UpdateTimeoutOnClient(Time.deltaTime);

        if (channel.isActive)
        {
            if (serverLinker.state != ServerLinker.ELinkerState.READY)
            {
                serverLinker.Update(Time.deltaTime);
            }
        }

        UpdateTimeoutOnServer(Time.deltaTime);

        performanceAverage.Update(Time.deltaTime);
        performanceTimer += Time.deltaTime;

        if (performanceTimer > 1.0f)
        {
            float average = performanceAverage.GetAverage();

            if (average <= 0.0f)
            {
                average += 0.001f;
            }

            float frameRate = 1.0f / average;
            Debug.Log(frameRate);

            performanceTimer -= 1.0f;

            Debug.Log("WRITE " + Mathf.RoundToInt(PerformanceStats.GetInstance().writeData.GetAverage()) + "ms");
            Debug.Log("READ " + Mathf.RoundToInt(PerformanceStats.GetInstance().readData.GetAverage()) + "ms");
        }
    }

    public void FindProxy(MiddleClientInfo client)
    {
        if (client.proxy != null)
        {
            return;
        }

        if (client.proxyId >= 0)
        {
            Entity entity = entityManager.GetEntityFromId((uint)client.proxyId);

            if (entity != null)
            {
                client.proxy = entity as PlayerEntity;
            }
        }
    }

    public void Tick()
    {
        //keep any connection alive
        if (channel.isActive && clients.Count <= 0)
        {
            SendPingToServer();
        }

        ProcessNetworkMessagesOnClient();
        ProcessNetworkMessagesOnServer();

        foreach (Entity entity in entityManager.entities)
        {
            bool isActive = !entity.TickTimeout();
            entity.SetActive(isActive);
        }

        ApplyParenting();

        SendReplicationData();

        channel.ResendReliableMessages(Time.deltaTime);

        foreach (MiddleClientInfo client in clients)
        {
            client.channel.ResendReliableMessages(Time.deltaTime);
        }

        localTick++;

        if (localTick >= Settings.PARTIAL_TO_FULL_RATIO)
        {
            localTick = 0;
        }
    }

    public void ApplyParenting()
    {
        foreach (Entity e in entityManager.entities)
        {
            if (e.type != EObject.PLAYER)
            {
                continue;
            }

            PlayerEntity player = e as PlayerEntity;

            if (player.parentId < 0)
            {
                if (player.transform.parent != null)
                {
                    Quaternion previousFrame = player.frame;

                    Vector3 position = transform.localPosition;

                    player.transform.SetParent(null);
                    player.frame = Quaternion.identity;

                    transform.localPosition = position;
                }
            }
            else
            {
                Entity entity = entityManager.GetEntityFromId((uint)player.parentId);

                if (entity == null)
                {
                    if (player.transform.parent != null)
                    {
                        Quaternion previousFrame = player.frame;

                        Vector3 position = transform.localPosition;

                        player.transform.SetParent(null);
                        player.frame = Quaternion.identity;

                        transform.localPosition = position;
                    }
                }
                else
                {
                    if (player.transform.parent == null)
                    {
                        Quaternion previousFrame = player.frame;

                        Vector3 position = transform.localPosition;

                        player.transform.SetParent(entity.transform);
                        player.frame = entity.transform.rotation;

                        transform.localPosition = position;
                    }
                    else
                    {
                        player.frame = entity.transform.rotation;
                    }
                }
            }
        }
    }

    public byte[] CompressData(ref byte[] buffer, int bytesWritten)
    {
        //compress buffer
        int maxCompressionLength = entropyEncoder.MaxCompressionSize(bytesWritten);

        BitStream compressedStream = new BitStream(maxCompressionLength);
        entropyEncoder.WriteCompressedBytes(ref compressedStream, buffer);

        //copy compressed buffer into final byte array
        int compressedBitsWritten = compressedStream.bitIndex & 0x7;
        int compressedBytesWritten = compressedStream.bitIndex >> 3;

        if (compressedBitsWritten > 0)
        {
            compressedBytesWritten++;
        }

        byte[] compressedBuffer = new byte[compressedBytesWritten];
        System.Buffer.BlockCopy(compressedStream.buffer, 0, compressedBuffer, 0, compressedBytesWritten);

        return compressedBuffer;
    }

    public byte[] DecompressData(ref BitStream stream)
    {
        byte[] decompressedBuffer = entropyEncoder.ReadCompressedBytes(ref stream);
        return decompressedBuffer;
    }

    public void SendHelloToServer(int id, int platformType)
    {
        MiddleClientInfo client = clients[id];

        BitStream stream = new BitStream(5);

        stream.WriteInt((int)EServerPacket.HELLO, 3);
        stream.WriteInt(client.ticket, Settings.MAX_TICKET_BITS);
        stream.WriteInt((int)platformType, 3);

        channel.Send(stream.buffer, Settings.EMessageType.RELIABLE_UNORDERED);
    }

    public void SendHelloToClient(int id)
    {
        MiddleClientInfo client = clients[id];

        BitStream stream = new BitStream(5);

        stream.WriteInt((int)EServerPacket.HELLO, 3);
        stream.WriteInt(client.proxyId, Settings.MAX_ENTITY_BITS);

        byte[] compressedBuffer = CompressData(ref stream.buffer, 5);

        client.channel.Send(compressedBuffer, Settings.EMessageType.RELIABLE_UNORDERED);
    }

    public void SendPingToServer()
    {
        BitStream stream = new BitStream(1);
        stream.WriteInt((int)EClientPacket.PING, 3);

        channel.Send(stream.buffer, Settings.EMessageType.UNRELIABLE);
    }

    public void SendPreferencesToServer(int id, ref BitStream readingStream)
    {
        MiddleClientInfo client = clients[id];

        //reset stream index after reading it ourselves
        readingStream.bitIndex = 3;

        //we have already read the header, read the rest
        byte[] rest = readingStream.ReadBits(readingStream.buffer.Length * 8 - 3);

        BitStream stream = new BitStream(readingStream.buffer.Length + 5);

        stream.WriteInt((int)EClientPacket.PREFERENCES, 3);
        stream.WriteInt(client.ticket, Settings.MAX_TICKET_BITS);
        stream.WriteBytes(rest);
        channel.Send(stream.buffer, Settings.EMessageType.RELIABLE_UNORDERED);
    }

    public void SendInputToServer(int id, ref BitStream readingStream)
    {
        MiddleClientInfo client = clients[id];

        //we have already read the header, read the rest
        byte[] rest = readingStream.ReadBits(readingStream.buffer.Length * 8 - 3);

        BitStream stream = new BitStream(readingStream.buffer.Length + 5);

        stream.WriteInt((int)EClientPacket.INPUT, 3);
        stream.WriteInt(client.ticket, Settings.MAX_TICKET_BITS);
        stream.WriteBytes(rest);
        channel.Send(stream.buffer, Settings.EMessageType.UNRELIABLE);
    }

    public void SendDisconnectToServer(int id)
    {
        MiddleClientInfo client = clients[id];

        BitStream stream = new BitStream(5);
        stream.WriteInt((int)EClientPacket.DISCONNECT, 3);
        stream.WriteInt(client.ticket, Settings.MAX_TICKET_BITS);

        channel.Send(stream.buffer, Settings.EMessageType.RELIABLE_UNORDERED);
    }

    public int OUTSTANDING_THREADS = 0;

    public void SendReplicationData()
    {
        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
        watch.Start();

        EReplication replicationMode = EReplication.UPDATE;

        OUTSTANDING_THREADS = 0;

        int count = clients.Count;

        if (replicationMode == EReplication.UPDATE)
        {
            entityManager.CacheTransformData();
            entityManager.CacheBits(localTick);

            for (int i = 0; i < count; i++)
            {
                MiddleClientInfo client = clients[i];

                if (client.proxy == null)
                {
                    FindProxy(client);
                }
            }

            OUTSTANDING_THREADS = count;

            for (int i = 0; i < count; i++)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(WriteEntityDataOnThread), i);
            }
        }

        //hang until threads complete
        while (OUTSTANDING_THREADS > 0)
        {
            Thread.VolatileRead(ref OUTSTANDING_THREADS);
        }

        entityManager.FinishWritingReplicationData();
        entityManager.ClearDirtyFlags();

        watch.Stop();
        long ms = watch.ElapsedMilliseconds;

        PerformanceStats.GetInstance().writeData.Update(ms);
    }

    public void WriteEntityDataOnThread(object state)
    {
        int MAX_PACKETS = 10;

        WriteReplicationIndexer indexer = new WriteReplicationIndexer();

        MiddleClientInfo client = clients[(int)state];

        //prepare culling group
        CullingStack cullingStack = entityManager.referenceCullingStack.Clone();

        if (client.proxy != null)
        {
            //prepare culling group
            cullingStack.group.Add(client.proxy);

            //at the current stage, this is far too laggy to run efficiently
            //entityManager.SetPriorities(client.proxy);
        }

        int LIMIT = Mathf.CeilToInt(client.preferences.entityCount.value * (float)Settings.MAX_ENTITY_INDEX) + Settings.MINIMUM_ENTITY_LIMIT;

        //write unreliable packets UPDATE
        for (int packet = 0; packet < MAX_PACKETS; packet++)
        {
            BitStream stream = new BitStream(Settings.BUFFER_LIMIT + 6);
            stream.WriteInt((int)EServerPacket.REPLICATION, 3);

            indexer = entityManager.WriteCachedReplicationDataIndexed(ref stream, ref cullingStack, indexer, localTick, LIMIT);

            int bitsWritten = stream.bitIndex & 0x7;
            int bytesWritten = stream.bitIndex >> 3;

            if (bitsWritten > 0)
            {
                bytesWritten++;
            }

            byte[] trimmedBuffer = new byte[bytesWritten];

            System.Buffer.BlockCopy(stream.buffer, 0, trimmedBuffer, 0, bytesWritten);

            //compress buffer
            byte[] compressedBuffer = CompressData(ref trimmedBuffer, bytesWritten);

            client.channel.Send(compressedBuffer, Settings.EMessageType.UNRELIABLE_ORDERED);

            if (indexer.isDone)
            {
                break;
            }
        }

        Interlocked.Decrement(ref OUTSTANDING_THREADS);
    }

    public void UpdateTimeoutOnClient(float deltaTime)
    {
        idleTime += deltaTime;

        if (idleTime >= Settings.TIMEOUT)
        {
            channel.Dump();
            channel = new Channel();
            channel.sendMessageCallback = StoreRecievedMessageOnClient;

            clientBindingPort = clientBaseBindingPort - Random.Range(0, 5000);

            //clear states
            clientMessageBuffer = new List<RecievedMessage>();
            idleTime = 0.0f;

            entityManager.Dump();

            //reset
            IPAddress linkerIp = IPOperation.ResolveDomainName(linkerAddress);

            clientLinker.Reset(linkerIp, linkerPort, clientBindingPort);
            clientLinker.Initialise();
            clientLinker.SendConnectClientRequest();

            int count = clients.Count;

            for (int i = 0; i < count; i++)
            {
                HandleDisconnectOnServer(0);
            }

            clients.Clear();
        }
    }

    public void UpdateTimeoutOnServer(float deltaTime)
    {
        int count = clients.Count;

        for (int i = 0; i < count; i++)
        {
            MiddleClientInfo client = clients[i];

            client.idleTime += deltaTime;

            if (client.idleTime >= Settings.TIMEOUT)
            {
                SendDisconnectToServer(i);
                HandleDisconnectOnServer(i);

                i--;
                count--;
            }
        }
    }

    public void Recieve()
    {
        int count = clients.Count;

        for (int i = 0; i < count; i++)
        {
            MiddleClientInfo client = clients[i];

            if (client.channel.isBusy)
            {
                return;
            }

            client.channel.Recieve();
        }
    }

    public void ProcessNetworkMessagesOnClient()
    {
        lock (clientMessageBuffer)
        {
            int messageCount = clientMessageBuffer.Count;

            if (messageCount > 0)
            {
                idleTime = 0.0f;
            }

            foreach (RecievedMessage message in clientMessageBuffer)
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
                        //recieved response from server, grab the proxy
                        int ticket = stream.ReadInt(Settings.MAX_TICKET_BITS);
                        int proxyId = stream.ReadInt(Settings.MAX_ENTITY_BITS);

                        int index;
                        MiddleClientInfo client = GetClientWithTicket(ticket, out index);

                        if (index >= 0)
                        {
                            client.proxyId = proxyId;
                            SendHelloToClient(index);
                        }
                    }
                    else if (packetType == EServerPacket.REPLICATION)
                    {
                        EReplicationDestination dummyDestination = (EReplicationDestination)stream.ReadInt(2);

                        if (dummyDestination == EReplicationDestination.UNDIRECTED)
                        {
                            entityManager.ReadReplicationData(ref stream);

                            foreach (MiddleClientInfo client in clients)
                            {
                                client.channel.Send(message.buffer, Settings.EMessageType.RELIABLE_UNORDERED);
                            }
                        }
                        else if (dummyDestination == EReplicationDestination.TO_CLIENT)
                        {
                            int ticket = stream.ReadInt(Settings.MAX_TICKET_BITS);

                            int index;
                            MiddleClientInfo client = GetClientWithTicket(ticket, out index);

                            if (index >= 0)
                            {
                                client.channel.Send(message.buffer, Settings.EMessageType.RELIABLE_UNORDERED);
                            }
                        }
                        else if (dummyDestination == EReplicationDestination.TO_MIDDLE)
                        {
                            entityManager.ReadReplicationData(ref stream);
                        }
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
                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();

                        entityManager.ReadReplicationData(ref stream);

                        watch.Stop();
                        long ms = watch.ElapsedMilliseconds;

                        PerformanceStats.GetInstance().readData.Update(ms);

                        //foreach (MiddleClientInfo client in clients)
                        //{
                        //    client.channel.Send(message.buffer, Settings.EMessageType.UNRELIABLE_ORDERED);
                        //}
                    }
                    else if (packetType == EServerPacket.PROXY)
                    {
                        int ticket = stream.ReadInt(Settings.MAX_TICKET_BITS);

                        int index;
                        MiddleClientInfo client = GetClientWithTicket(ticket, out index);

                        if (index >= 0)
                        {
                            client.channel.Send(message.buffer, Settings.EMessageType.UNRELIABLE_ORDERED);
                        }
                    }
                }
            }

            clientMessageBuffer.Clear();
        }
    }


    public void ProcessNetworkMessagesOnServer()
    {
        int count = clients.Count;

        for (int i = 0; i < count; i++)
        {
            ConcurrentQueue<RecievedMessage> miniBuffer = serverMessageBuffer[i];

            MiddleClientInfo client = clients[i];

            int messageCount = miniBuffer.Count;

            //reset the timeout timer
            if (messageCount > 0)
            {
                client.idleTime = 0.0f;
            }

            RecievedMessage message;

            //process all messages
            for (int j = 0; j < messageCount; j++)
            {
                miniBuffer.TryDequeue(out message);

                if (message == null)
                {
                    break;
                }

                if (message.type == Settings.EMessageType.UNRELIABLE)
                {
                    BitStream stream = new BitStream(message.buffer);

                    byte packetByte = stream.ReadBits(3)[0];
                    EClientPacket packetType = (EClientPacket)packetByte;

                    if (packetType == EClientPacket.PING)
                    {
                        channel.Send(message.buffer, Settings.EMessageType.UNRELIABLE);
                    }
                    else if (packetType == EClientPacket.HELLO)
                    {
                        int platformType = stream.ReadInt(3);

                        //relay client ticket to server
                        SendHelloToServer(i, platformType);
                    }
                    else if (packetType == EClientPacket.INPUT)
                    {
                        SendInputToServer(i, ref stream);
                    }
                }
                else if (message.type == Settings.EMessageType.RELIABLE_UNORDERED)
                {
                    BitStream stream = new BitStream(message.buffer);

                    byte packetByte = stream.ReadBits(3)[0];

                    EClientPacket packetType = (EClientPacket)packetByte;

                    if (packetType == EClientPacket.PREFERENCES)
                    {
                        //note the preferences, then relay to server
                        client.preferences.ReadFromStream(ref stream);

                        SendPreferencesToServer(i, ref stream);
                    }
                    else if (packetType == EClientPacket.DISCONNECT)
                    {
                        //relay disconnect to server
                        SendDisconnectToServer(i);
                        HandleDisconnectOnServer(i);

                        i--;
                        count--;
                        break; //get out of removed minibuffer
                    }
                }
            }

            if (miniBuffer != null)
            {
                miniBuffer.Clear();
            }
        }
    }

    public void StoreRecievedMessageOnClient(RecievedMessage message)
    {
        lock (clientMessageBuffer)
        {
            clientMessageBuffer.Add(message);

            int count = clientMessageBuffer.Count;

            if (count > Settings.MAX_MESSAGE_QUEUE_SIZE)
            {
                //find the earliest unreliable data
                for (int i = 0; i < count; i++)
                {
                    if (clientMessageBuffer[i].type == Settings.EMessageType.UNRELIABLE)
                    {
                        clientMessageBuffer.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    public void StoreRecievedMessageOnServer(RecievedMessage message)
    {
        int count = serverMessageBuffer.Count;

        for (int i = 0; i < count; i++)
        {
            if (clients[i].channel == message.owner)
            {
                ConcurrentQueue<RecievedMessage> miniBuffer = serverMessageBuffer[i];

                //check for null reference from HandleDisconnect()
                if (miniBuffer == null)
                {
                    return;
                }

                int messageCount = miniBuffer.Count;

                if (messageCount < Settings.MAX_MESSAGE_QUEUE_SIZE)
                {
                    miniBuffer.Enqueue(message);
                }

                break;
            }
        }
    }
}
