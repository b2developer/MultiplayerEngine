using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ObjectRegistry : MonoBehaviour
{
    public delegate void PlayerFunc(Entity entity);

    public bool includeInterpolationFilter = true;

    public GameObject[] serverPrefabs;
    public GameObject[] clientPrefabs;
    public GameObject playerAnimator;

    public EntityManager entityManager;

    public PlayerFunc spawnPlayerCallback;
    public PlayerFunc destroyPlayerCallback;
    public PlayerFunc spawnShipCallback;
    public PlayerFunc destroyShipCallback;

    public void Initialise()
    {
        spawnPlayerCallback = DefaultPlayerFuncCallback;
        destroyPlayerCallback = DefaultPlayerFuncCallback;
        spawnShipCallback = DefaultPlayerFuncCallback;
        destroyShipCallback = DefaultPlayerFuncCallback;
    }

    public void DefaultPlayerFuncCallback(Entity entity)
    {

    }

    public void RegisterObjectClient(ref BitStream stream)
    {
        Entity entity = CreateObjectClient(ref stream);

        if (entity.type == EObject.PLAYER)
        {
            spawnPlayerCallback(entity);
        }
        else if (entity.type == EObject.SHIP)
        {
            spawnShipCallback(entity);
        }
        else if (entity.type == EObject.BIG_SHIP)
        {
            spawnShipCallback(entity);
        }
        else if (entity.type == EObject.SMALL_SHIP)
        {
            spawnShipCallback(entity);
        }

        entityManager.lookup.Place(entity, (int)entity.id);

        entity.Initialise();
        entityManager.entities.Add(entity);
    }

    public Entity CreateObjectClient(ref BitStream stream)
    {
        stream.bitIndex += Settings.MAX_ENTITY_BITS;
        EObject type = (EObject)stream.ReadInt(Settings.MAX_TYPE_BITS);
        stream.bitIndex -= Settings.MAX_ENTITY_BITS + Settings.MAX_TYPE_BITS;

        switch (type)
        {
            case EObject.CUBE: return CreateCubeClient(ref stream);
            case EObject.BALL: return CreateBallClient(ref stream);
            case EObject.SPINNER: return CreateSpinnerClient(ref stream);
            case EObject.CHAMELEON: return CreateChameleonClient(ref stream);
            case EObject.PLAYER: return CreatePlayerClient(ref stream);
            case EObject.PROJECTILE: return CreateProjectileClient(ref stream);
            case EObject.SHIP: return CreateShipClient(ref stream, EObject.SHIP);
            case EObject.BIG_SHIP: return CreateShipClient(ref stream, EObject.BIG_SHIP);
            case EObject.SMALL_SHIP: return CreateShipClient(ref stream, EObject.SMALL_SHIP);
        }

        return null;
    }

    public void RegisterObjectServer(Entity entity)
    {
        if (entity.type == EObject.PLAYER)
        {
            spawnPlayerCallback(entity);
        }
        else if (entity.type == EObject.SHIP)
        {
            spawnShipCallback(entity);
        }
        else if (entity.type == EObject.BIG_SHIP)
        {
            spawnShipCallback(entity);
        }
        else if (entity.type == EObject.SMALL_SHIP)
        {
            spawnShipCallback(entity);
        }

        entity.id = entityManager.networkIds.GetFreeIndex();
        entityManager.AssignFullTick(entity);

        entityManager.lookup.Place(entity, (int)entity.id);

        entity.Initialise();

        entityManager.entities.Add(entity);
    }

    //implementations of object spawners
    //----------

    public Entity CreateCubeClient(ref BitStream stream)
    {
        GameObject instance = Instantiate<GameObject>(clientPrefabs[(int)EObject.CUBE]);

        TransformEntity transformEntity = instance.GetComponent<TransformEntity>();

        if (includeInterpolationFilter)
        {
            transformEntity.interpolationFilter = new InterpolationFilter();
        }
        
        transformEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        transformEntity.rotation = new CompressedQuaternion();

        transformEntity.ReadFromStream(ref stream);

        instance.transform.SetPositionAndRotation(transformEntity.position.vector, transformEntity.rotation.quaternion);

        if (includeInterpolationFilter)
        {
            //finish interpolation for teleportation
            transformEntity.interpolationFilter.timer = Settings.INTERPOLATION_PERIOD;
        }        

        return transformEntity;
    }

    public Entity CreateBallClient(ref BitStream stream)
    {
        GameObject instance = Instantiate<GameObject>(clientPrefabs[(int)EObject.BALL]);

        TransformEntity transformEntity = instance.GetComponent<TransformEntity>();

        if (includeInterpolationFilter)
        {
            transformEntity.interpolationFilter = new InterpolationFilter();
        }
        
        transformEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        transformEntity.rotation = new CompressedQuaternion();

        transformEntity.ReadFromStream(ref stream);

        instance.transform.SetPositionAndRotation(transformEntity.position.vector, transformEntity.rotation.quaternion);

        if (includeInterpolationFilter)
        {
            //finish interpolation for teleportation
            transformEntity.interpolationFilter.timer = Settings.INTERPOLATION_PERIOD;
        }
        
        return transformEntity;
    }

    public Entity CreateSpinnerClient(ref BitStream stream)
    {
        GameObject instance = Instantiate<GameObject>(clientPrefabs[(int)EObject.SPINNER]);

        TransformEntity transformEntity = instance.GetComponent<TransformEntity>();

        if (includeInterpolationFilter)
        {
            transformEntity.interpolationFilter = new InterpolationFilter();
        }
        
        transformEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        transformEntity.rotation = new CompressedQuaternion();

        transformEntity.ReadFromStream(ref stream);

        instance.transform.SetPositionAndRotation(transformEntity.position.vector, transformEntity.rotation.quaternion);

        if (includeInterpolationFilter)
        {
            //finish interpolation for teleportation
            transformEntity.interpolationFilter.timer = Settings.INTERPOLATION_PERIOD;
        }

        return transformEntity;
    }

    public Entity CreateChameleonClient(ref BitStream stream)
    {
        GameObject instance = Instantiate<GameObject>(clientPrefabs[(int)EObject.CHAMELEON]);

        ChameleonEntity chameleonEntity = instance.GetComponent<ChameleonEntity>();

        if (includeInterpolationFilter)
        {
            chameleonEntity.interpolationFilter = new InterpolationFilter();
        }
       
        chameleonEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        chameleonEntity.rotation = new CompressedQuaternion();

        chameleonEntity.ReadFromStream(ref stream);

        instance.transform.SetPositionAndRotation(chameleonEntity.position.vector, chameleonEntity.rotation.quaternion);

        if (includeInterpolationFilter)
        {
            //finish interpolation for teleportation
            chameleonEntity.interpolationFilter.timer = Settings.INTERPOLATION_PERIOD;
        }

        return chameleonEntity;
    }

    public Entity CreatePlayerClient(ref BitStream stream)
    {
        GameObject instance = Instantiate<GameObject>(clientPrefabs[(int)EObject.PLAYER]);

        PlayerEntity playerEntity = instance.GetComponent<PlayerEntity>();

        if (includeInterpolationFilter)
        {
            playerEntity.interpolationFilter = new InterpolationFilter();
        }
        
        playerEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        playerEntity.rotation = new CompressedQuaternion();

        playerEntity.ReadFromStream(ref stream);

        instance.transform.SetPositionAndRotation(playerEntity.position.vector, playerEntity.rotation.quaternion);

        if (includeInterpolationFilter)
        {
            //finish interpolation for teleportation
            playerEntity.interpolationFilter.timer = Settings.INTERPOLATION_PERIOD;
        }

        return playerEntity;
    }

    public Entity CreateProjectileClient(ref BitStream stream)
    {
        GameObject instance = Instantiate<GameObject>(clientPrefabs[(int)EObject.PROJECTILE]);

        TransformEntity transformEntity = instance.GetComponent<TransformEntity>();

        if (includeInterpolationFilter)
        {
            transformEntity.interpolationFilter = new InterpolationFilter();
        }

        transformEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        transformEntity.rotation = new CompressedQuaternion();

        transformEntity.ReadFromStream(ref stream);

        instance.transform.SetPositionAndRotation(transformEntity.position.vector, transformEntity.rotation.quaternion);

        if (includeInterpolationFilter)
        {
            //finish interpolation for teleportation
            transformEntity.interpolationFilter.timer = Settings.INTERPOLATION_PERIOD;
        }
        
        return transformEntity;
    }

    public Entity CreateShipClient(ref BitStream stream, EObject variant)
    {
        GameObject instance = Instantiate<GameObject>(clientPrefabs[(int)variant]);

        ShipClientEntity shipEntity = instance.GetComponent<ShipClientEntity>();

        if (includeInterpolationFilter)
        {
            shipEntity.interpolationFilter = new InterpolationFilter();
        }

        shipEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        shipEntity.rotation = new CompressedQuaternion();

        shipEntity.ReadFromStream(ref stream);

        instance.transform.SetPositionAndRotation(shipEntity.position.vector, shipEntity.rotation.quaternion);

        if (includeInterpolationFilter)
        {
            //finish interpolation for teleportation
            shipEntity.interpolationFilter.timer = Settings.INTERPOLATION_PERIOD;
        }

        return shipEntity;
    }

    public void CreateCubeServer(Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate<GameObject>(serverPrefabs[(int)EObject.CUBE]);

        instance.transform.SetPositionAndRotation(position, rotation);

        TransformEntity transformEntity = instance.GetComponent<TransformEntity>();

        transformEntity.type = EObject.CUBE;

        transformEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        transformEntity.rotation = new CompressedQuaternion();

        transformEntity.position.vector = position;
        transformEntity.rotation.quaternion = rotation;

        RegisterObjectServer(transformEntity);
    }

    public void CreateBallServer(Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate<GameObject>(serverPrefabs[(int)EObject.BALL]);

        instance.transform.SetPositionAndRotation(position, rotation);

        TransformEntity transformEntity = instance.GetComponent<TransformEntity>();

        transformEntity.type = EObject.BALL;

        transformEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        transformEntity.rotation = new CompressedQuaternion();

        transformEntity.position.vector = position;
        transformEntity.rotation.quaternion = rotation;

        RegisterObjectServer(transformEntity);
    }

    public void CreateSpinnerServer(Vector3 position, Quaternion rotation, Vector3 angularVelocity)
    {
        GameObject instance = Instantiate<GameObject>(serverPrefabs[(int)EObject.SPINNER]);

        instance.transform.SetPositionAndRotation(position, rotation);

        SpinnerEntity spinnerEntity = instance.GetComponent<SpinnerEntity>();

        spinnerEntity.type = EObject.SPINNER;

        spinnerEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        spinnerEntity.rotation = new CompressedQuaternion();

        spinnerEntity.position.vector = position;
        spinnerEntity.rotation.quaternion = rotation;
        spinnerEntity.angularVelocity = angularVelocity;

        RegisterObjectServer(spinnerEntity);
    }

    public void CreateChameleonServer(Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate<GameObject>(serverPrefabs[(int)EObject.CHAMELEON]);

        instance.transform.SetPositionAndRotation(position, rotation);

        ChameleonEntity chameleonEntity = instance.GetComponent<ChameleonEntity>();

        chameleonEntity.type = EObject.CHAMELEON;

        chameleonEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        chameleonEntity.rotation = new CompressedQuaternion();

        chameleonEntity.position.vector = position;
        chameleonEntity.rotation.quaternion = rotation;

        RegisterObjectServer(chameleonEntity);
    }

    public void CreatePlayerServer(Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate<GameObject>(serverPrefabs[(int)EObject.PLAYER]);

        instance.transform.SetPositionAndRotation(position, rotation);

        PlayerEntity playerEntity = instance.GetComponent<PlayerEntity>();

        playerEntity.type = EObject.PLAYER;

        playerEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        playerEntity.rotation = new CompressedQuaternion();

        playerEntity.position.vector = position;
        playerEntity.rotation.quaternion = rotation;

        RegisterObjectServer(playerEntity);
    }

    public void AddPlayerAnimator(PlayerEntity entity)
    {
        MeshRenderer[] meshRenderers = entity.GetComponentsInChildren<MeshRenderer>(true);

        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.enabled = false;
        }

        entity.nameTag.SetActive(false);
       
        GameObject instance = Instantiate<GameObject>(playerAnimator);

        instance.transform.SetPositionAndRotation(entity.position.vector, entity.rotation.quaternion);

        TransformEntity transformEntity = instance.GetComponent<TransformEntity>();

        entity.animator = transformEntity;

        transformEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        transformEntity.rotation = new CompressedQuaternion();

        transformEntity.position.vector = entity.position.vector;
        transformEntity.rotation = entity.rotation;

        transformEntity.interpolationFilter = new InterpolationFilter();
        transformEntity.interpolationFilter.enableRotation = false;

        //sweap out default cosmetics for player animator cosmetics
        PlayerCosmetics cosmetics = instance.GetComponentInChildren<PlayerCosmetics>();

        cosmetics.SetRace(entity.cosmetics.race);

        Destroy(entity.cosmetics);
        entity.cosmetics = cosmetics;

    }

    public void RemovePlayerAnimator(PlayerEntity entity)
    {
        Destroy(entity.animator.gameObject);
        entity.animator = null;
    }

    public void CreateProjectileServer(Vector3 position, Quaternion rotation)
    {
        GameObject instance = Instantiate<GameObject>(serverPrefabs[(int)EObject.PROJECTILE]);

        instance.transform.SetPositionAndRotation(position, rotation);

        ProjectileEntity projectileEntity = instance.GetComponent<ProjectileEntity>();

        projectileEntity.type = EObject.PROJECTILE;

        projectileEntity.entityManager = entityManager;

        projectileEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        projectileEntity.rotation = new CompressedQuaternion();

        projectileEntity.position.vector = position;
        projectileEntity.rotation.quaternion = rotation;

        RegisterObjectServer(projectileEntity);
    }

    public void CreateShipServer(Vector3 position, Quaternion rotation, Vector3 angularVelocity, EObject variant)
    {
        GameObject instance = Instantiate<GameObject>(serverPrefabs[(int)variant]);

        instance.transform.SetPositionAndRotation(position, rotation);

        ShipEntity shipEntity = instance.GetComponent<ShipEntity>();

        shipEntity.type = variant;

        shipEntity.position = new FixedPoint3(Settings.WORLD_MIN_X, Settings.WORLD_MIN_Y, Settings.WORLD_MIN_Z, Settings.WORLD_MAX_X, Settings.WORLD_MAX_Y, Settings.WORLD_MAX_Z, 0.01f);
        shipEntity.rotation = new CompressedQuaternion();

        shipEntity.position.vector = position;
        shipEntity.rotation.quaternion = rotation;

        RegisterObjectServer(shipEntity);
    }
}
