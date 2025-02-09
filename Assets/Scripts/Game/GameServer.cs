using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class GameServer : MonoBehaviour
{
    public Server server;
    public AudioSource source;
    public PlanetFinder finder;

    public static float DEATH_Y = -80.0f;
    
    public Transform[] spawns;

    public List<PlayerEntity> activePlayers;
    public List<ShipEntity> activeShips;
    public List<ClientInfo> activeInfos;

    public Transform statue;

    public float STATUE_DISTANCE = 1.0f;

    public float musicTimer = 0.0f;
    public float downtime = 5.0f;

    public void Initialise()
    {
        activePlayers = new List<PlayerEntity>();
        activeShips = new List<ShipEntity>();

        playerSpeeds = new List<Vector3>();
        playerAngulars = new List<Vector3>();

        shipSpeeds = new List<Vector3>();
        shipAngulars = new List<Vector3>();
    }

    public void Tick()
    {
        musicTimer -= Time.fixedDeltaTime;

        if (musicTimer < 0.0f)
        {
            int selected = Random.Range(2, 12);

            AudioClip clip = server.rpcManager.clips[selected];

            musicTimer += clip.length + downtime;

            server.rpcManager.PlaySoundServer(selected);
        }

        List<PlayerEntity> nearStatue = new List<PlayerEntity>();

        int playerCount = activePlayers.Count;
        int shipCount = activeShips.Count;

        for (int i = 0; i < playerCount; i++)
        {
            PlayerEntity player = activePlayers[i];

            float distanceToStatue = (statue.position - player.transform.position).sqrMagnitude;

            if (distanceToStatue <= STATUE_DISTANCE * STATUE_DISTANCE)
            {
                nearStatue.Add(player);
            }

            if (player.transform.position.y < DEATH_Y || player.transform.position.y > -DEATH_Y)
            {
                player.transform.position = spawns[player.cosmetics.race].position;
                player.body.linearVelocity = Vector3.zero;

                if (player.parentId >= 0)
                {
                    ShipEntity ship = server.entityManager.GetEntityFromId((uint)player.parentId) as ShipEntity;

                    if (ship != null)
                    {
                        if (ship.controller == player)
                        {
                            ship.controller = null;
                        }
                    }

                    player.parentId = -1;
                    player.frame = Quaternion.identity;
                    player.isRouted = false;
                    player.transform.SetParent(null);

                    continue;
                }
            }

            if (player.parentId < 0)
            {
                for (int j = 0; j < shipCount; j++)
                {
                    ShipEntity ship = activeShips[j];

                    Vector3 centre = ship.transform.TransformPoint(ship.inside.center);
                    Vector3 size = new Vector3(ship.transform.localScale.x * ship.inside.size.x, ship.transform.localScale.y * ship.inside.size.y, ship.transform.localScale.z * ship.inside.size.z);

                    bool test = MathExtension.PointInsideBox(player.transform.position, centre, ship.transform.rotation, size);

                    if (test)
                    {
                        if (ship.type != EObject.SMALL_SHIP || (ship.type == EObject.SMALL_SHIP && player.cosmetics.isGold))
                        {
                            player.parentId = (int)ship.id;
                            player.transform.SetParent(ship.tail.transform);
                            player.frame = ship.transform.rotation;

                            player.BaseTick();
                        }
                    }
                }
            }
            else
            {
                ShipEntity ship = server.entityManager.GetEntityFromId((uint)player.parentId) as ShipEntity;

                if (ship == null)
                {
                    player.parentId = -1;
                    player.transform.SetParent(null);
                    player.frame = Quaternion.identity;
                    player.isRouted = false;

                    player.BaseTick();
                }
                else
                {

                    Vector3 centre = ship.transform.TransformPoint(ship.inside.center);
                    Vector3 size = new Vector3(ship.transform.localScale.x * ship.inside.size.x, ship.transform.localScale.y * ship.inside.size.y, ship.transform.localScale.z * ship.inside.size.z);

                    bool test = MathExtension.PointInsideBox(player.transform.position, centre, ship.transform.rotation, size);

                    if (!test)
                    {
                        player.parentId = -1;
                        player.transform.SetParent(null);
                        player.frame = Quaternion.identity;

                        player.isRouted = false;

                        if (ship.controller == player)
                        {
                            ship.controller = null;
                        }

                        player.BaseTick();
                    }
                    else
                    {
                        player.frame = ship.transform.rotation;

                        Vector3 driveCentre = ship.drive.transform.TransformPoint(ship.drive.center);
                        Vector3 driveSize = new Vector3(ship.drive.transform.localScale.x * ship.drive.size.x, ship.drive.transform.localScale.y * ship.drive.size.y, ship.drive.transform.localScale.z * ship.drive.size.z);

                        bool driveTest = MathExtension.PointInsideBox(player.transform.position, driveCentre, ship.transform.rotation, driveSize);

                        if (driveTest && player.airTimer < 0.2f)
                        {
                            if (ship.controller == null)
                            {
                                player.isRouted = true;
                                ship.controller = player;
                            }
                        }
                        else
                        {
                            if (ship.controller == player)
                            {
                                player.isRouted = false;
                                ship.controller = null;
                            }
                        }
                    }
                }
            }
        }

        bool[] checks = new bool[10];

        for (int i = 0; i < 10; i++)
        {
            checks[i] = false;
        }

        for (int i = 0; i < nearStatue.Count; i++)
        {
            checks[nearStatue[i].cosmetics.race] = true;
        }

        bool success = true;

        for (int i = 0; i < 10; i++)
        {
            if (!checks[i])
            {
                success = false;
                break;
            }
        }

        if (success)
        {
            for (int i = 0; i < nearStatue.Count; i++)
            {
                nearStatue[i].cosmetics.isGold = true;
            }
        }
    }

    public List<Vector3> playerSpeeds;
    public List<Vector3> playerAngulars;

    public void RememberPlayerState()
    {
        playerSpeeds.Clear();
        playerAngulars.Clear();

        int count = activePlayers.Count;

        for (int i = 0; i < count; i++)
        {
            PlayerEntity player = activePlayers[i];
            playerSpeeds.Add(player.body.linearVelocity);
            playerAngulars.Add(player.body.angularVelocity);
        }
    }

    public void RestorePlayerState()
    {
        int count = activePlayers.Count;

        for (int i = 0; i < count; i++)
        {
            PlayerEntity player = activePlayers[i];
            player.body.linearVelocity = playerSpeeds[i];
            player.body.angularVelocity = playerAngulars[i];
        }

        playerSpeeds.Clear();
        playerAngulars.Clear();
    }

    public List<Vector3> shipSpeeds;
    public List<Vector3> shipAngulars;

    public void RememberShipState()
    {
        shipSpeeds.Clear();
        shipAngulars.Clear();

        int count = activeShips.Count;

        for (int i = 0; i < count; i++)
        {
            ShipEntity ship = activeShips[i];
            shipSpeeds.Add(ship.body.linearVelocity);
            shipAngulars.Add(ship.body.angularVelocity);
        }
    }

    public void RestoreShipState()
    {
        int count = activeShips.Count;

        for (int i = 0; i < count; i++)
        {
            ShipEntity ship = activeShips[i];
            ship.body.linearVelocity = shipSpeeds[i];
            ship.body.angularVelocity = shipAngulars[i];
        }

        shipSpeeds.Clear();
        shipAngulars.Clear();
    }

    public void SwitchPhysicsMode(bool enablePlayers, bool enableShips)
    {
        foreach (PlayerEntity player in activePlayers)
        {
            player.body.isKinematic = !enablePlayers;
            player.body.detectCollisions = enablePlayers;
        }

        foreach (ShipEntity ship in activeShips)
        {
            ship.body.isKinematic = !enableShips;
        }
    }

    public void UpdateTails()
    {
        foreach (ShipEntity ship in activeShips)
        {
            ship.tailScript.Tick();
        }
    }

    public void OnPlayerSpawned(Entity entity)
    {
        PlayerEntity activePlayer = entity as PlayerEntity;

        activePlayers.Add(activePlayer);

        foreach (MiddleInfo middle in server.middles)
        {
            foreach (ClientInfo client in middle.clients)
            {
                if (client.proxy.id == entity.id)
                {
                    activeInfos.Add(client);
                }
            }
        }

        activePlayer.cosmetics.race = Random.Range(0, 10);
        activePlayer.transform.position = spawns[activePlayer.cosmetics.race].position;
    }

    public void OnPlayerDestroyed(Entity entity)
    {
        PlayerEntity activePlayer = entity as PlayerEntity;

        if (activePlayer.parentId >= 0)
        {
            ShipEntity ship = server.entityManager.GetEntityFromId((uint)activePlayer.parentId) as ShipEntity;

            if (ship != null)
            {
                if (ship.controller == activePlayer)
                {
                    ship.controller = null;
                }
            }

            activePlayer.parentId = -1;
            activePlayer.frame = Quaternion.identity;
            activePlayer.isRouted = false;
            activePlayer.transform.SetParent(null);
        }

        activePlayers.Remove(activePlayer);

        foreach (MiddleInfo middle in server.middles)
        {
            foreach (ClientInfo client in middle.clients)
            {
                if (client == null)
                {
                    activeInfos.Remove(client);
                }
            }
        }
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
