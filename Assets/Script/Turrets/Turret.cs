using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Turret : MonoBehaviour
{
    [HideInInspector] public TurretData turretData;

    [Header("Stats")]
    public float range;
    public float fireRate;
    public float damage;
    public int cost;

    [Header("Upgrade Info")]
    public int upgradeLevel = 1;
    public int maxUpgradeLevel = 15;

    public Transform currentTarget;
    private float fireCountdown = 0f;

    public GameObject projectilePrefab;
    public Transform firePoint;

    private LineRenderer lineRenderer;
    public int circleSegments = 60;

    public string turretName = "Standard Turret";

    public TurretInfoUI turretInfoUI;
    public bool isSelected = false;
    public enum TargetingMode { Closest, Farthest, HighestHealth, LowestHealth }
    public TargetingMode currentTargetingMode = TargetingMode.Closest;
    protected virtual void Start()
    {
        if (turretInfoUI != null)
            turretInfoUI.Initialize(this);
    }

    public void UpdateRangeCircle()
    {
        float angleStep = 360f / circleSegments;
        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angle) * range;
            float z = Mathf.Sin(angle) * range;
            lineRenderer.SetPosition(i, new Vector3(x, 0.01f, z));
        }
    }

    void Update()
    {
        if (turretData != null && turretData.type == "support")
            return;

        FindEnemyByMode(); 
        if (currentTarget == null) return;

        fireCountdown -= Time.deltaTime * GameSpeedController.SpeedMultiplier;

        if (fireCountdown <= 0f)
        {
            Shoot();
            fireCountdown = 1f / fireRate;
        }
    }
    public void ApplyData(TurretData data)
    {
        this.turretData = data;

        this.range = data.range * GameStats.TurretRangeMultiplier;

        this.fireRate = Mathf.Max(0.01f, data.fireRate);

        this.damage = data.damage * GameStats.TurretDamageMultiplier;

        this.cost = data.cost;
        this.turretName = data.name;

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = circleSegments + 1;
                lineRenderer.loop = true;
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.enabled = false;
            }
        }

        UpdateRangeCircle();
    }



    void FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector3.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance && distanceToEnemy <= range)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        currentTarget = nearestEnemy != null ? nearestEnemy.transform : null;
    }

    protected virtual void Shoot()
    {
        GameObject projectile = ProjectilePool.Instance.GetProjectile();
        projectile.transform.position = firePoint.position;
        projectile.transform.rotation = firePoint.rotation;
        projectile.GetComponent<Projectile>().Initialize(currentTarget, this);
    }

    void OnMouseDown()
    {
        bool wasAlreadySelected = TurretSelector.Instance.IsSelected(this);

        TurretSelector.Instance.SelectTurret(this);

        if (wasAlreadySelected)
        {
            HideRangeLine();
            if (turretInfoUI != null)
                turretInfoUI.Hide();
        }
        else
        {
            ShowRangeLine(); 
            if (turretInfoUI != null)
            {
                turretInfoUI.UpdateInfo();
                turretInfoUI.Show();
            }
        }
    }


    public void ShowRangeLine()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = true;

    }

    public void HideRangeLine()
    {
        if (lineRenderer != null || turretInfoUI != null)
        {
            lineRenderer.enabled = false;
            turretInfoUI.Hide();

        }
    }

    void FindEnemyByMode()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        var enemiesInRange = enemies
            .Where(e => Vector3.Distance(transform.position, e.transform.position) <= range)
            .ToList();

        if (!enemiesInRange.Any())
        {
            currentTarget = null;
            return;
        }

        switch (currentTargetingMode)
        {
            case TargetingMode.Closest:
                QuickSort(enemiesInRange, (a, b) =>
                    Vector3.Distance(transform.position, a.transform.position)
                    .CompareTo(Vector3.Distance(transform.position, b.transform.position)),
                    0, enemiesInRange.Count - 1);
                currentTarget = enemiesInRange[0].transform;
                break;

            case TargetingMode.Farthest:
                QuickSort(enemiesInRange, (a, b) =>
                    Vector3.Distance(transform.position, b.transform.position)
                    .CompareTo(Vector3.Distance(transform.position, a.transform.position)),
                    0, enemiesInRange.Count - 1);
                currentTarget = enemiesInRange[0].transform;
                break;

            case TargetingMode.HighestHealth:
                QuickSort(enemiesInRange, (a, b) =>
                    b.GetComponent<Enemy>().maxHealth
                    .CompareTo(a.GetComponent<Enemy>().maxHealth),
                    0, enemiesInRange.Count - 1);
                currentTarget = enemiesInRange[0].transform;
                break;

            case TargetingMode.LowestHealth:
                QuickSort(enemiesInRange, (a, b) =>
                    a.GetComponent<Enemy>().maxHealth
                    .CompareTo(b.GetComponent<Enemy>().maxHealth),
                    0, enemiesInRange.Count - 1);
                currentTarget = enemiesInRange[0].transform;
                break;
        }
    }

    public void ChangeTargetingMode()
    {
        currentTargetingMode = (TargetingMode)(((int)currentTargetingMode + 1) % System.Enum.GetValues(typeof(TargetingMode)).Length);
        turretInfoUI.UpdateTargetModeText(currentTargetingMode);

        currentTarget = null; 
        FindEnemyByMode();
    }
    public static void QuickSort<T>(List<T> list, System.Func<T, T, int> compare, int left, int right)
    {
        if (left < right)
        {
            int pivotIndex = Partition(list, compare, left, right);
            QuickSort(list, compare, left, pivotIndex - 1);
            QuickSort(list, compare, pivotIndex + 1, right);
        }
    }

    static int Partition<T>(List<T> list, System.Func<T, T, int> compare, int left, int right)
    {
        T pivot = list[right];
        int i = left - 1;
        for (int j = left; j < right; j++)
        {
            if (compare(list[j], pivot) < 0)
            {
                i++;
                // Swap
                var temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
        // Swap pivot
        var temp2 = list[i + 1];
        list[i + 1] = list[right];
        list[right] = temp2;
        return i + 1;
    }


}
