using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class SentMessage
{
    public int sequence = 0;
    public byte[] buffer;
    public float time = 0.0f;

    public SentMessage()
    {
        buffer = new byte[0];
    }
}

public class RecievedMessage
{
    public byte[] buffer;
    public int bufferLength = 0;
    public Settings.EMessageType type;
    public EndPoint origin;

    public Channel owner;

    public RecievedMessage(byte[] _buffer, int _bufferLength, Settings.EMessageType _type, EndPoint _origin = null, Channel _owner = null)
    {
        buffer = new byte[_bufferLength];
        System.Buffer.BlockCopy(_buffer, 0, buffer, 0, _bufferLength);
        bufferLength = _bufferLength;
        type = _type;
        origin = _origin;

        owner = _owner;
    }
}

public class Channel
{
    public delegate void ByteFunc(int length);
    public delegate void MessageFunc(RecievedMessage message);

    public MessageFunc sendMessageCallback;
    public ByteFunc sentBytesCallback;
    public ByteFunc recievedBytesCallback;

    public byte[] buffer;
    public int bufferLength = 0;
    public Settings.EMessageType messageType;
    public EndPoint origin;

    public Socket socket;
    public EndPoint endpoint;

    public bool isActive = false;
    public bool isBusy = false;
    
    public bool dynamicPort = false;

    //for reliable udp
    public List<SentMessage> pendingMessages;
    public int reliableSendIndex = 0;
    public int reliableRecievedIndex = 0;
    public int ackHistory = 0;

    //for unreliable ordered udp
    public int orderedSendIndex = 0;
    public int orderedRecievedIndex = -1;

    public ArtificialLag lag = null;

    public Channel()
    {
        endpoint = new IPEndPoint(IPAddress.Any, 0);

        pendingMessages = new List<SentMessage>();

        ackHistory = 0;

        buffer = new byte[Settings.BUFFER_SIZE];

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;

        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        //ignore connection reset errors since this is a udp connection
        //uint IOC_IN = 0x80000000;
        //uint IOC_VENDOR = 0x18000000;
        //uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
        //socket.IOControl((int)SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);

        sentBytesCallback = DefaultBytesFuncCallback;
        recievedBytesCallback = DefaultBytesFuncCallback;
    }

    public void DefaultBytesFuncCallback(int length)
    {

    }

    public void Bind(string ipString, int port)
    {
        socket.Bind(new IPEndPoint(IPAddress.Parse(ipString), port));
        isActive = true;
    }

    public void Bind(EndPoint ep)
    {
        socket.Bind(ep);
        isActive = true;
    }

    public void Connect(string ipString, int port)
    {
        //socket.Connect(new IPEndPoint(IPAddress.Parse(ipString), port));
        endpoint = new IPEndPoint(IPAddress.Parse(ipString), port);
    }

    public void Connect(EndPoint ep)
    {
        endpoint = ep;
    }

    public void Dump()
    {
        if (!isActive)
        {
            return;
        }

        //close the socket
        lock (socket)
        {
            isActive = false;
            socket.Close(0);
        }
    }

    //simple loop to start recieving automatically
    public void RecieveThread()
    {
        while (true)
        {
            if (isBusy)
            {
                continue;
            }

            //kill thread, socket goes null after Dump()
            if (!isActive)
            {
                break;
            }

            Recieve();
        }
    }

    public void Recieve(bool bypass = false)
    {
        if (isBusy && !bypass || !isActive)
        {
            return;
        }
         
        origin = new IPEndPoint(IPAddress.Any, 0);
        socket.BeginReceiveFrom(buffer, 0, Settings.BUFFER_SIZE, SocketFlags.None, ref origin, OnDataRecieved, null);

        isBusy = true;
    }

    public void OnDataRecieved(IAsyncResult result)
    {
        if (!isActive)
        {
            isBusy = false;
            return;
        }

        int recieved = 0;

        lock (socket)
        {
            recieved = socket.EndReceiveFrom(result, ref origin);
        }

        //empty buffer check
        //----------
        if (recieved == 0)
        {
            //recursive loop
            Recieve(true);

            return;
        }
        //----------

        recievedBytesCallback(recieved + Settings.UDP_OVERHEAD);

        bufferLength = recieved;

        //security check
        //----------
        bool verified = Hash.VerifyBuffer(ref buffer, bufferLength);

        //check for a correct signature
        if (!verified)
        {
            //recursive loop
            Recieve(true);

            return;
        }
        //----------

        //symmetric NAT handling
        //----------
        if (dynamicPort)
        {
            IPEndPoint sendIp = endpoint as IPEndPoint;
            IPEndPoint recievedIp = origin as IPEndPoint;

            bool matchingIP = IPAddress.Equals(sendIp.Address, recievedIp.Address) || IPOperation.IsLocal(recievedIp.Address);

            if (matchingIP)
            {
                if (sendIp.Port != recievedIp.Port)
                {
                    //symmetric NAT has assigned a different port, adjust
                    endpoint = new IPEndPoint(sendIp.Address, recievedIp.Port);

                    UnityEngine.Debug.Log("SYMMETRIC NAT DETECTED");
                }
            }
        }
        //----------

        BitStream stream = new BitStream(buffer);

        byte header = stream.ReadBits(2)[0];
        byte type = (byte)((int)header & 0x3);

        messageType = (Settings.EMessageType)type;

        //unreliable
        if (type < 1)
        {
            stream.bitIndex = 8;
            int remaining = recieved * 8 - 8;
            byte[] inBuffer = stream.ReadBits(remaining);

            System.Buffer.BlockCopy(inBuffer, 0, buffer, 0, bufferLength - 1);

            RecievedMessage message = new RecievedMessage(buffer, bufferLength, messageType, origin, this);
            sendMessageCallback(message);

            //recursive loop
            Recieve(true);

            return;
        }

        if (messageType == Settings.EMessageType.RELIABLE_UNORDERED)
        {
            //reliable unordered or an ack
            int ackIndex = stream.ReadInt(14);

            int difference = MathExtension.DiffWrapped(reliableRecievedIndex, ackIndex, Settings.MAX_ACK_NUMBER);

            int setMask = 1;

            //keep track of highest message recieved
            if (MathExtension.IsGreaterWrapped(ackIndex, reliableRecievedIndex, Settings.MAX_ACK_NUMBER))
            {
                //note the difference, to update the bitmask
                reliableRecievedIndex = ackIndex;

                //shift mask to scroll to most recent message
                ackHistory = ackHistory << difference;
            }
            else
            {
                //not the highest message, mark it at the right spot
                setMask = 1 << difference;
            }

            int checkMask = ackHistory & setMask;

            //check that this is the first time recieving this message
            if (checkMask == 0)
            {
                //add new ack flag to the mask
                ackHistory = ackHistory | setMask;

                stream.bitIndex = 16;
                int remaining = recieved * 8 - 16;
                byte[] inBuffer = stream.ReadBits(remaining);

                System.Buffer.BlockCopy(inBuffer, 0, buffer, 0, bufferLength - 2);

                RecievedMessage message = new RecievedMessage(buffer, bufferLength, messageType, origin, this);
                sendMessageCallback(message);
            }

            //always send back an ack
            SendAcknowledgement(ackIndex);
        }
        else if (messageType == Settings.EMessageType.RELIABLE_ACK)
        {
            //reliable unordered or an ack
            int ackIndex = stream.ReadInt(14);
            int ackField = stream.ReadInt(32);

            lock (pendingMessages)
            {
                //remove the ack from our sent messages
                int count = pendingMessages.Count;

                for (int i = 0; i < count; i++)
                {
                    SentMessage item = pendingMessages[i];

                    int difference = MathExtension.DiffWrapped(item.sequence, ackIndex, Settings.MAX_ACK_NUMBER);

                    int mask = 1;

                    //check if the mask is valid
                    if (difference > 0 && difference <= Settings.ACK_FIELD_SIZE)
                    {
			            difference--;

                        mask = mask << difference;

                        int andMask = mask & ackField;

                        if (andMask > 0)
                        {
                            pendingMessages.RemoveAt(i);
                            i--;
                            count--;

                            UnityEngine.Debug.Log("ACK RECIEVED THROUGH MASK");
                        }
                    }
                    else if (item.sequence == ackIndex)
                    {
                        //found match, remove it from resends
                        pendingMessages.RemoveAt(i);
                        i--;
                        count--;

                        UnityEngine.Debug.Log("ACK RECIEVED DIRECTLY");
                    }
                    if (difference > Settings.ACK_FIELD_SIZE)
                    {
                        //something has gone horribly wrong, 32 ack bits was not enough to acknowledge the message once :(
                        UnityEngine.Debug.Log("ACK WAS NOT RECIEVED IN FIELD (older than 32 messages)");
                    }
                }
            }
 
            //not worth responding to past the low-level
            bufferLength = 0;
        }
        else if (messageType == Settings.EMessageType.UNRELIABLE_ORDERED)
        {
            int recievedIndex = stream.ReadInt(6);

            bool isOrdered = MathExtension.IsGreaterWrapped(recievedIndex, orderedRecievedIndex, Settings.MAX_INDEX_NUMBER);

            //check if packet arrived in order
            if (isOrdered)
            {
                orderedRecievedIndex = recievedIndex;

                stream.bitIndex = 8;
                int remaining = recieved * 8 - 8;
                byte[] inBuffer = stream.ReadBits(remaining);

                System.Buffer.BlockCopy(inBuffer, 0, buffer, 0, bufferLength - 1);

                RecievedMessage message = new RecievedMessage(buffer, bufferLength, messageType, origin, this);
                sendMessageCallback(message);
            }
            else
            {
                UnityEngine.Debug.Log("DROPPED OUT OF ORDER PACKET");
            }
        }

        //recursive loop
        Recieve(true);
    }

    public void Send(byte[] data, Settings.EMessageType type)
    {
        if (type == Settings.EMessageType.UNRELIABLE)
        {
            int length = data.GetLength(0);

            byte[] dataWithHeader = new byte[length + 1];
            System.Buffer.BlockCopy(data, 0, dataWithHeader, 1, length);

            BitStream stream = new BitStream(dataWithHeader);

            stream.WriteBits(0x0, 2);
            BeginSend(stream.buffer);
        }
        else if (type == Settings.EMessageType.RELIABLE_UNORDERED)
        {
            int length = data.GetLength(0);

            byte[] dataWithHeader = new byte[length + 2];
            System.Buffer.BlockCopy(data, 0, dataWithHeader, 2, length);

            BitStream stream = new BitStream(dataWithHeader);

            stream.WriteBits(0x1, 2);
            stream.WriteInt(reliableSendIndex, 14);

            BeginSend(stream.buffer);

            SentMessage sent = new SentMessage();
            sent.buffer = stream.buffer;
            sent.sequence = reliableSendIndex;

            lock (pendingMessages)
            {
                pendingMessages.Add(sent);
            }

            reliableSendIndex++;

            //wrap around index
            if (reliableSendIndex >= Settings.MAX_ACK_NUMBER)
            {
                reliableSendIndex -= Settings.MAX_ACK_NUMBER;
            }
        }
        else if (type == Settings.EMessageType.UNRELIABLE_ORDERED)
        {
            int length = data.GetLength(0);

            byte[] dataWithHeader = new byte[length + 1];
            System.Buffer.BlockCopy(data, 0, dataWithHeader, 1, length);

            BitStream stream = new BitStream(dataWithHeader);

            stream.WriteBits(0x3, 2);
            stream.WriteInt(orderedSendIndex, 6);

            BeginSend(stream.buffer);

            orderedSendIndex++;

            //wrap around index
            if (orderedSendIndex >= Settings.MAX_INDEX_NUMBER)
            {
                orderedSendIndex -= Settings.MAX_INDEX_NUMBER;
            }
        }
        else if (type == Settings.EMessageType.RAW)
        {
            BeginSend(data);
        }
    }

    public void SendAcknowledgement(int index)
    {
        BitStream stream = new BitStream(6);
        stream.WriteBits(0x2, 2);
        stream.WriteInt(index, 14);

        int sentHistory = ackHistory;
        int difference = MathExtension.DiffWrapped(reliableRecievedIndex, index, Settings.MAX_ACK_NUMBER);

        //set the stored mask to the correct position relative to the index we are sending
        if (difference > 0)
        {
            sentHistory = sentHistory >> difference;
        }

        stream.WriteInt(sentHistory, 32);

        BeginSend(stream.buffer);

        SentMessage sent = new SentMessage();
        sent.buffer = stream.buffer;
        sent.sequence = index;
    }

    public void BeginSend(byte[] buffer)
    {
        if (lag == null)
        {
            sentBytesCallback(buffer.Length + Settings.UDP_OVERHEAD);

            byte[] signedBuffer = Hash.SignBuffer(buffer);
            socket.BeginSendTo(signedBuffer, 0, signedBuffer.Length, SocketFlags.None, endpoint, OnDataSendTo, null);
        }
        else
        {
            SentMessage sent = new SentMessage();
            sent.buffer = buffer;

            lag.QueueMessage(sent);
        }
    }

    public void OnDataSendTo(IAsyncResult result)
    {
        if (!isActive)
        {
            return;
        }

        lock (socket)
        {
            socket.EndSendTo(result);
        }
    }
    
    public void ResendReliableMessages(float deltaTime)
    {
        lock (pendingMessages)
        {
            int count = pendingMessages.Count;

            for (int i = 0; i < count; i++)
            {
                SentMessage item = pendingMessages[i];

                item.time += deltaTime;

                if (item.time >= Settings.RELIABLE_TIMEOUT)
                {
                    UnityEngine.Debug.Log("LOSS DETECTED, RESENDING");

                    Send(item.buffer, Settings.EMessageType.RAW);
                    item.time -= Settings.RELIABLE_TIMEOUT;
                }
            }
        }
    }
}