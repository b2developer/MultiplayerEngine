using UnityEngine;

public class PlanetFinder : MonoBehaviour
{
    public static PlanetFinder instance = null;

    public Client client;

    public Transform[] planets;

    public Material dualSkybox;
    public Material[] skyboxes;

    public int currentIndex = 0;

    public float radius = 10.0f;
    public float height = 5.0f;
    public float transition = 5.0f;

    public Transform[] gravitySources;
    public float[] strengths;
    public float fieldDistance = 20.0f;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Update()
    {
        if (client == null)
        {
            return;
        }

        if (client.proxy == null)
        {
            return;
        }

        Vector3 position = client.proxy.animator.transform.position;

        int count = planets.Length;

        for (int i = 0; i < count; i++)
        {
            Transform planet = planets[i];

            //test intersection
            if (MathExtension.PointInsideCylinder(position, planet.position, radius, height))
            {
                //set to skybox
                SetSkybox(skyboxes[i], i);
                dualSkybox.SetFloat("_Lerp", 1.0f);
                break;
            }
            else
            {
                Vector3 support = MathExtension.ClosestPointOnCylinder(position, planet.position, radius, height);

                float distance = (support - position).magnitude;

                if (distance < transition)
                {
                    float lerp = 1.0f - distance / transition;

                    SetSkybox(skyboxes[i], i);
                    dualSkybox.SetFloat("_Lerp", lerp);

                    break;
                }
            }
        }

        
    }

    public float GetGravity(PlayerEntity player)
    {
        int count = gravitySources.Length;

        for (int i = 0; i < count; i++)
        {
            Transform gravity = gravitySources[i];

            //test intersection
            if (MathExtension.PointInsideCylinder(player.transform.position, gravity.position, radius, height))
            {
                return strengths[i];
            }
            else
            {
                Vector3 support = MathExtension.ClosestPointOnCylinder(player.transform.position, gravity.position, radius, height);

                float distance = (support - player.transform.position).magnitude;

                if (distance < fieldDistance)
                {
                    return strengths[i];
                }
            }
        }

        return 0.0f;
    }

    public void SetSkybox(Material skybox, int index)
    {
        if (index == currentIndex)
        {
            return;
        }

        Color bottomColor = skybox.GetColor("_BottomColor");
        Color middleColor = skybox.GetColor("_MiddleColor");
        Color topColor = skybox.GetColor("_TopColor");

        float split1 = skybox.GetFloat("_Split1");
        float split2 = skybox.GetFloat("_Split2");
        float split3 = skybox.GetFloat("_Split3");

        dualSkybox.SetColor("_BottomColor2", bottomColor);
        dualSkybox.SetColor("_MiddleColor2", middleColor);
        dualSkybox.SetColor("_TopColor2", topColor);

        dualSkybox.SetFloat("_Split12", split1);
        dualSkybox.SetFloat("_Split22", split2);
        dualSkybox.SetFloat("_Split32", split3);

        currentIndex = index;
    }
}
