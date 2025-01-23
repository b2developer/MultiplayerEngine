using UnityEngine;

public class PerformanceStats
{
    public static PerformanceStats instance = null;

    public static PerformanceStats GetInstance()
    {
        if (instance == null)
        {
            instance = new PerformanceStats();
        }

        return instance;
    }

    public MovingAverage writeData;
    public MovingAverage readData;

    public PerformanceStats()
    {
        writeData = new MovingAverage(50);
        readData = new MovingAverage(50);
    }
}
