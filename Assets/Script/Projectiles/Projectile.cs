using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private Turret sourceTurret;
    public float speed = 10f;

    private bool isActive = false;

    [Header("Special Effects")]
    public bool isSlowing = false;
    public float slowAmount;
    public float slowDuration;

    public bool isAOE = false;
    public float explosionRadius = 0f;

    // Método llamado desde la torreta al disparar
    public void Initialize(Transform targetEnemy, Turret turret)
    {
        target = targetEnemy;
        sourceTurret = turret;
        isActive = true;
    }

    void Update()
    {
        if (!isActive || target == null)
        {
            ReturnToPool();
            return;
        }

        Vector3 direction = target.position - transform.position;
        float distanceThisFrame = speed * Time.deltaTime;

        if (direction.magnitude <= distanceThisFrame)
        {
            HitTarget(target.gameObject);
            return;
        }

        transform.Translate(direction.normalized * distanceThisFrame, Space.World);
    }

    void HitTarget(GameObject targetObject)
    {
        if (isAOE)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    ApplyDamage(hit.GetComponent<Enemy>());
                }
            }
        }
        else
        {
            ApplyDamage(target.GetComponent<Enemy>());
        }

        // Aplicar slow si corresponde
        if (isSlowing && target != null)
        {
            Enemy e = target.GetComponent<Enemy>();
            //e.ApplySlow(slowAmount, slowDuration);
        }

        ReturnToPool();
    }

    void ReturnToPool()
    {
        isActive = false;
        target = null;
        sourceTurret = null;
        ProjectilePool.Instance.ReturnProjectile(gameObject);
    }
    private void ApplyDamage(Enemy enemy)
    {
        if (enemy == null) return;

        float damageToApply = sourceTurret.damage; // Daño de la torreta que disparó

        enemy.TakeDamage(damageToApply); // Suponiendo que Enemy.cs tiene ese método

        // Registrar daño total hecho
        sourceTurret.totalDamageDealt += damageToApply;
    }

}
