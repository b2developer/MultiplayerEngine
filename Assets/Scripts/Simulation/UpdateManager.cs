using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//simple singleton class that helps regulate updates
public class UpdateManager : MonoBehaviour
{
    public delegate void TickFunction();

    public static UpdateManager instance = null;
    public TickFunction preFunction;
    public TickFunction entityFunction;
    public TickFunction managerFunction;
    public TickFunction postFunction;

    public float timer = 0.0f;
    public float maxTimer = 0.2f;

    public void Initialise()
    {
        instance = this;

        preFunction += DefaultFunction;
        entityFunction += DefaultFunction;
        managerFunction += DefaultFunction;
        postFunction += DefaultFunction;
    }

    void Update()
    {
        //if there is lag, don't overload the simulation with update ticks
        timer += Mathf.Min(Time.deltaTime, Time.fixedDeltaTime);

        //prevent accumulative timer from getting too high
        timer = Mathf.Min(timer, maxTimer);

        if (timer > Time.fixedDeltaTime)
        {
            preFunction();

            Physics.SyncTransforms();

            entityFunction();
            managerFunction();
        }
    }

    void LateUpdate()
    {
        if (timer > Time.fixedDeltaTime)
        {
            postFunction();
            timer -= Time.fixedDeltaTime;
        }
    }

    public void DefaultFunction()
    {

    }
}
