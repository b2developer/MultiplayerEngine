using System.Collections.Generic;
using UnityEngine;

public class GameClient : MonoBehaviour
{
    public Client client;
    public PlanetFinder finder;

    public List<ShipEntity> activeShips;

    public void Initialise()
    {
        activeShips = new List<ShipEntity>();
    }

    public void Tick()
    { 
        int playerCount = client.players.Count;

        for (int i = 0; i < playerCount; i++)
        {
            PlayerEntity player = client.players[i];

            if (player.parentId < 0)
            {
                if (player.transform.parent != null)
                {
                    Quaternion previousFrame = player.frame;

                    player.transform.SetParent(null);
                    player.frame = Quaternion.identity;

                    if (player.animator != null)
                    {
                        player.animator.interpolationFilter.previousPosition = player.animator.transform.parent.TransformPoint(player.animator.interpolationFilter.previousPosition);
                        player.animator.interpolationFilter.previousRotation = player.animator.interpolationFilter.previousRotation * player.animator.transform.parent.rotation;
                        player.animator.interpolationFilter.currentPosition = player.animator.transform.parent.TransformPoint(player.animator.interpolationFilter.currentPosition);
                        player.animator.interpolationFilter.currentRotation = player.animator.interpolationFilter.previousRotation * player.animator.transform.parent.rotation;

                        player.animator.interpolationFilter.Apply(player.transform);

                        player.animator.transform.SetParent(null);
                    }

                    player.cameraCorrectionCallback(previousFrame, player.frame);
                }
            }
            else
            {
                Entity entity = client.entityManager.GetEntityFromId((uint)player.parentId);

                if (entity == null)
                {
                    if (player.transform.parent != null)
                    {
                        Quaternion previousFrame = player.frame;

                        player.transform.SetParent(null);
                        player.frame = Quaternion.identity;

                        if (player.animator != null)
                        {
                            player.animator.interpolationFilter.previousPosition = player.animator.transform.parent.TransformPoint(player.animator.interpolationFilter.previousPosition);
                            player.animator.interpolationFilter.previousRotation = player.animator.interpolationFilter.previousRotation * player.animator.transform.parent.rotation;
                            player.animator.interpolationFilter.currentPosition = player.animator.transform.parent.TransformPoint(player.animator.interpolationFilter.currentPosition);
                            player.animator.interpolationFilter.currentRotation = player.animator.interpolationFilter.previousRotation * player.animator.transform.parent.rotation;

                            player.animator.interpolationFilter.Apply(player.transform);

                            player.animator.transform.SetParent(null);
                        }

                        player.cameraCorrectionCallback(previousFrame, player.frame);
                    }
                }
                else
                {
                    if (player.transform.parent == null)
                    {
                        Quaternion previousFrame = player.frame;

                        player.transform.SetParent(entity.transform);
                        player.frame = entity.transform.rotation;

                        if (player.animator != null)
                        {
                            player.animator.transform.SetParent(entity.transform);

                            player.animator.interpolationFilter.previousPosition = player.animator.transform.parent.InverseTransformPoint(player.animator.interpolationFilter.previousPosition);
                            player.animator.interpolationFilter.previousRotation = Quaternion.Inverse(player.animator.transform.parent.rotation) * player.animator.interpolationFilter.previousRotation;
                            player.animator.interpolationFilter.currentPosition = player.animator.transform.parent.InverseTransformPoint(player.animator.interpolationFilter.currentPosition);
                            player.animator.interpolationFilter.currentRotation = Quaternion.Inverse(player.animator.transform.parent.rotation) * player.animator.interpolationFilter.previousRotation;

                            player.animator.interpolationFilter.Apply(player.transform);
                        }

                        player.cameraCorrectionCallback(previousFrame, player.frame);
                    }
                    else
                    {
                        player.frame = entity.transform.rotation;
                    }
                }
            }
        }

        if (client.proxy == null)
        {
            return;
        }

        client.inputManager.cameraController.isRouted = client.proxy.isRouted;
    }

    public void OnShipSpawned(Entity entity)
    {
        activeShips.Add(entity as ShipEntity);
    }

    public void OnShipDestroyed(Entity entity)
    {
        activeShips.Remove(entity as ShipEntity);
    }
}
