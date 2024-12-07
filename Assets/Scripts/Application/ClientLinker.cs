using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class ClientLinker
{
    public delegate void ReadyFunc(int bindingPort, IPAddress remoteIp, int remotePort);
   
    public enum ELinkerState
    {
        WAITING = 0,
        READY = 1,
    }

    public ELinkerState state;

    public ReadyFunc readyFunction;

    public Channel channel;

    public IPAddress remoteIp;
    public int remotePort = 0;
    public int bindingPort = 0;

    public bool isMiddle = false;

    public bool forcePublicIp = false;
    public IPAddress publicIp;

    public List<RecievedMessage> messageBuffer;

    public float timer = 0.0f;

    public ClientLinker(ReadyFunc _readyFunction, IPAddress _remoteIp, int _remotePort, int _bindingPort)
    {
        channel = new Channel();
        channel.sendMessageCallback = StoreRecievedMessage;

        readyFunction = _readyFunction;
        remoteIp = _remoteIp;
        remotePort = _remotePort;
        bindingPort = _bindingPort;

        messageBuffer = new List<RecievedMessage>();
    }

    public void Reset(IPAddress _remoteIp, int _remotePort, int _bindingPort)
    {
        channel = new Channel();
        channel.sendMessageCallback = StoreRecievedMessage;

        remoteIp = _remoteIp;
        remotePort = _remotePort;
        bindingPort = _bindingPort;
    }

    public void Initialise()
    {
        channel.Bind(IPAddress.Any.ToString(), bindingPort);
        channel.Connect(remoteIp.ToString(), remotePort);

        //start the recursive loop
        channel.Recieve();
    }

    //send the initial request for connecting
    public void SendConnectClientRequest()
    {
        BitStream stream = new BitStream(5);
        stream.WriteInt((int)Settings.ELinkingStateType.CONNECT, 3);
        stream.WriteBool(isMiddle);
        stream.WriteBool(forcePublicIp);

        //write the public ip instead if the linker requested it
        if (forcePublicIp)
        {
            stream.WriteBytes(publicIp.GetAddressBytes());
        }

        channel.Send(stream.buffer, Settings.EMessageType.UNRELIABLE);

        state = ELinkerState.WAITING;
    }

    public void Update(float deltaTime)
    {
        ProcessNetworkMessages();

        timer += deltaTime;

        if (timer > Settings.CLIENT_LINKER_TIMEOUT)
        {
            timer -= Settings.CLIENT_LINKER_TIMEOUT;

            //resend actions if the linker did not respond after a certain amount of time
            if (state == ELinkerState.WAITING)
            {
                SendConnectClientRequest();
            }
        }
    }

    public void ProcessNetworkMessages()
    {
        lock (messageBuffer)
        {
            foreach (RecievedMessage message in messageBuffer)
            {
                if (message.type == Settings.EMessageType.UNRELIABLE)
                {
                    BitStream stream = new BitStream(message.buffer);

                    int stateInt = stream.ReadInt(3);

                    Settings.ELinkingStateType stateType = (Settings.ELinkingStateType)stateInt;

                    if (state == ELinkerState.WAITING && stateType == Settings.ELinkingStateType.LINK)
                    {
                        //link was recieved, read in the new address and start sending directly there
                        IPAddress address = new IPAddress(stream.ReadBits(32));
                        int port = stream.ReadInt(32);
                        int bindPort = stream.ReadInt(32);

                        channel.Dump();

                        //ready
                        state = ELinkerState.READY;
                        readyFunction(bindPort, address, port);
                    }
                    else if (state == ELinkerState.WAITING && stateType == Settings.ELinkingStateType.LOCAL_IP_DETECTED)
                    {
                        //linker detected a local ip, we must send a global one
                        forcePublicIp = true;
                        publicIp = IPOperation.GlobalIPAddress();

                        if (publicIp.Equals(IPAddress.Any))
                        {
                            forcePublicIp = false;
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
            RecievedMessage copy = new RecievedMessage(message.buffer, message.bufferLength, message.type);
            messageBuffer.Add(copy);
        }
    }
}
