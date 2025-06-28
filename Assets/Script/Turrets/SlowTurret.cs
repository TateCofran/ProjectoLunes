using UnityEngine;

public class SlowTurret : Turret
{
    protected override void Shoot()
    {
        if (currentTarget == null) return;

        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        projectile.transform.position = firePoint.position;
        projectile.transform.rotation = firePoint.rotation;

        var p = projectile.GetComponent<Projectile>();
        p.Initialize(currentTarget, this);

        // Marcar como proyectil que ralentiza
        p.isSlowing = true;
        p.slowAmount = turretData.slowAmount;
        p.slowDuration = turretData.slowDuration;
    }
}
