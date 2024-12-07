using UnityEngine;

public class ProjectileSpawner
{
    //references to components
    public EntityManager entityManager;
    public ObjectRegistry objectRegistry;

    public PlayerEntity playerEntity;

    public float forwardOffset = 0.25f - 0.05f - 1e-6f;

    public void Tick()
    {
        if (!playerEntity.isServer)
        {
            return;
        }

        if (playerEntity.input.fire.state == EButtonState.ON_PRESS)
        {
            Vector3 forward = -playerEntity.input.GetLookVector();

            objectRegistry.CreateProjectileServer(playerEntity.transform.position + forward * forwardOffset, playerEntity.transform.rotation);

            ProjectileEntity projectileEntity = entityManager.entities[^1] as ProjectileEntity;
            projectileEntity.ownerId = playerEntity.id;
            projectileEntity.velocity = projectileEntity.speed * forward;
        }
    }
}
