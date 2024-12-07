using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class ArtificialLag
{
    public System.Random rng;

    public Channel channel;

    public List<SentMessage> pendingMessages;
    public List<float> times;

    public float packetLoss = 0.95f;
    public float tripTime = 3.5f;
    public float additionalTripTime = 1.0f;

    public ArtificialLag()
    {
        rng = new System.Random();

        pendingMessages = new List<SentMessage>();
        times = new List<float>();
    }

    public void QueueMessage(SentMessage message)
    {
        float rand = (float)rng.NextDouble();
        float rand2 = (float)rng.NextDouble();

        //simulate packet loss
        if (rand > packetLoss)
        {
            return;
        }

        pendingMessages.Add(message);
        times.Add(tripTime + additionalTripTime * rand2);
    }

    public void Update(float deltaTime)
    {
        int count = pendingMessages.Count;

        for (int i = 0; i < count; i++)
        {
            SentMessage item = pendingMessages[i];

            times[i] -= deltaTime;

            //time has passed
            if (times[i] <= 0.0f)
            {
                byte[] signedBuffer = Hash.SignBuffer(item.buffer);
                channel.socket.BeginSendTo(signedBuffer, 0, signedBuffer.Length, SocketFlags.None, channel.endpoint, channel.OnDataSendTo, null);

                //remove the pending message
                pendingMessages.RemoveAt(i);
                times.RemoveAt(i);
                i--;
                count--;
            }
        }
    }
}
