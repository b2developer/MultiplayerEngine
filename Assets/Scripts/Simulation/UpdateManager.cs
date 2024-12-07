using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//simple singleton class that helps regulate updates
public class UpdateManager : MonoBehaviour
{
    public delegate void TickFunction();

    public static UpdateManager instance = null;
    public TickFunction entityFunction;
    public TickFunction managerFunction;

    public float timer = 0.0f;
    public float maxTimer = 0.2f;

    public void Initialise()
    {
        instance = this;

        entityFunction += DefaultFunction;
        managerFunction += DefaultFunction;
    }

    void Update()
    {
        //if there is lag, don't overload the simulation with update ticks
        timer += Mathf.Min(Time.deltaTime, Time.fixedDeltaTime);

        //prevent accumulative timer from getting too high
        timer = Mathf.Min(timer, maxTimer);

        if (timer > Time.fixedDeltaTime)
        {
            entityFunction();
            managerFunction();
            timer -= Time.fixedDeltaTime;
        }
    }

    public void DefaultFunction()
    {

    }
}
