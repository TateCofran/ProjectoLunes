using UnityEngine;

public class AOETurret : Turret
{
    protected override void Shoot()
    {
        if (currentTarget == null) return;

        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        projectile.transform.position = firePoint.position;
        projectile.transform.rotation = firePoint.rotation;

        var p = projectile.GetComponent<Projectile>();
        p.Initialize(currentTarget, this);

        // Marcar como proyectil de explosión
        p.isAOE = true;
        p.explosionRadius = turretData.explosionRadius;
    }
}
