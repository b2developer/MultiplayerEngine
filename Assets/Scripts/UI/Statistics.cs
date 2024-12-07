using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Statistics : MonoBehaviour
{
    public MovingAverage fpsAverage;
    public MovingAverage pingAverage;

    private int sentBytesCounter = 0;
    private int recievedBytesCounter = 0;

    public int sentBytes = 0;
    public int recievedBytes = 0;

    private float timer = 0.0f;

    void Start()
    {
        fpsAverage = new MovingAverage(20);
        pingAverage = new MovingAverage(20);
    }

    void Update()
    {
        float epsilon = 1e-6f;
        float fpsEstimate = 1.0f / Mathf.Max(Time.deltaTime, epsilon);

        fpsAverage.Update(fpsEstimate);

        timer += Time.deltaTime;

        if (timer >= 1.0f)
        {
            timer -= 1.0f;

            sentBytes = sentBytesCounter;
            recievedBytes = recievedBytesCounter;

            sentBytesCounter = 0;
            recievedBytesCounter = 0;
        }
    }

    public void OnPingRecieved(float time)
    {
        float ping = time * 1000.0f;
        pingAverage.Update(ping);
    }

    public void OnBytesSent(int length)
    {
        sentBytesCounter += length;
    }
    
    public void OnBytesRecieved(int length)
    {
        recievedBytesCounter += length;
    }
}
