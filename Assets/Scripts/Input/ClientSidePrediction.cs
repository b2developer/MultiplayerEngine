using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSidePrediction : MonoBehaviour
{
    public List<InputSample> unacknowledgedInputs;
    public PlayerEntity proxy = null;

    void Start()
    {
        unacknowledgedInputs = new List<InputSample>();
    }

    public void StoreInput(InputSample input)
    {
        InputSample clone = input.Clone();
        unacknowledgedInputs.Add(clone);

        if (unacknowledgedInputs.Count > Settings.MAX_CLIENT_SIDE_PREDICTION_SIZE)
        {
            unacknowledgedInputs.RemoveAt(0);
        }
    }

    public void ReconcileWithServer(int timestamp)
    {
        int count = unacknowledgedInputs.Count;
        int oldIndex = -1;

        //find matching input sample
        for (int i = 0; i < count; i++)
        {
            InputSample sample = unacknowledgedInputs[i];

            if (sample.timestamp == timestamp)
            {
                oldIndex = i;
                break;
            }
        }

        //if (oldIndex < 0)
        //{
        //    return;
        //}

        //remove all out of date inputs
        for (int i = 0; i <= oldIndex; i++)
        {
            unacknowledgedInputs.RemoveAt(0);
        }

        //re apply unacknowledged inputs
        count = unacknowledgedInputs.Count;

        for (int i = 0; i < count; i++)
        {
            proxy.input = unacknowledgedInputs[i];

            proxy.ManualTick();

            Physics.Simulate(Time.fixedDeltaTime);
            Physics.SyncTransforms();
        }
    }
}
