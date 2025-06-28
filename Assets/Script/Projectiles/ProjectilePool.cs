using UnityEngine;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance;

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int initialSize = 30;

    private ColaTF<GameObject> projectilePool = new ColaTF<GameObject>();
    private int totalInstantiated = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;

        projectilePool.InicializarCola();
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < initialSize; i++)
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.transform.SetParent(this.transform);
            projectile.SetActive(false);
            projectilePool.Acolar(projectile);
            totalInstantiated++;
        }
    }

    public GameObject GetProjectile()
    {
        if (projectilePool.ColaVacia())
        {
            GameObject projectile = Instantiate(projectilePrefab);
            projectile.transform.SetParent(this.transform);
            projectile.SetActive(false);
            projectilePool.Acolar(projectile);
            totalInstantiated++;
        }

        GameObject proj = projectilePool.Primero();
        projectilePool.Desacolar();
        proj.SetActive(true);
        return proj;
    }

    public void ReturnProjectile(GameObject projectile)
    {
        projectile.SetActive(false);
        projectile.transform.SetParent(this.transform);
        projectilePool.Acolar(projectile);
    }

    public void LogPoolStatus()
    {
        int unused = projectilePool.Count();
        int used = totalInstantiated - unused;
        Debug.Log($"Estado del Pool de Proyectiles: Total instanciados: {totalInstantiated}, Usados: {used}, Sin usar: {unused}");
    }
}

