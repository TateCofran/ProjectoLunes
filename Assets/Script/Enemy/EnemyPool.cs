using UnityEngine;
using System.Collections.Generic;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;

    [SerializeField] private GameObject slowEnemyPrefab;
    [SerializeField] private GameObject fastEnemyPrefab;
    [SerializeField] private GameObject miniBossPrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private int initialSizePerType = 10;

    public Dictionary<string, ColaTF<GameObject>> pools = new Dictionary<string, ColaTF<GameObject>>();
    private Dictionary<string, int> totalInstantiated = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance == null) Instance = this;

        // Inicializar colas por tipo
        pools["Slow"] = new ColaTF<GameObject>();
        pools["Fast"] = new ColaTF<GameObject>();
        pools["MiniBoss"] = new ColaTF<GameObject>();
        pools["Boss"] = new ColaTF<GameObject>();

        // Inicializar colas y contadores
        foreach (var pool in pools.Values)
        {
            pool.InicializarCola();
        }

        totalInstantiated["Slow"] = 0;
        totalInstantiated["Fast"] = 0;
        totalInstantiated["MiniBoss"] = 0;
        totalInstantiated["Boss"] = 0;

        // Inicializar objetos del pool
        InitializePool(slowEnemyPrefab, "Slow");
        InitializePool(fastEnemyPrefab, "Fast");
        InitializePool(miniBossPrefab, "MiniBoss", 2);
        InitializePool(bossPrefab, "Boss", 1);
    }

    void InitializePool(GameObject prefab, string type, int amount = -1)
    {
        int total = amount > 0 ? amount : initialSizePerType;

        for (int i = 0; i < total; i++)
        {
            GameObject enemy = Instantiate(prefab);
            enemy.transform.SetParent(this.transform);
            enemy.SetActive(false);
            pools[type].Acolar(enemy);
            totalInstantiated[type]++;
        }
    }

    public GameObject GetEnemy(string type)
    {
        ColaTF<GameObject> pool = pools[type];

        if (pool.ColaVacia())
        {
            GameObject prefab = type switch
            {
                "Slow" => slowEnemyPrefab,
                "Fast" => fastEnemyPrefab,
                "MiniBoss" => miniBossPrefab,
                "Boss" => bossPrefab,
                _ => slowEnemyPrefab,
            };

            GameObject enemy = Instantiate(prefab);
            enemy.transform.SetParent(this.transform);
            totalInstantiated[type]++;
            enemy.SetActive(true);
            return enemy;
        }
        else
        {
            GameObject enemyToSpawn = pool.Primero();
            pool.Desacolar();
            enemyToSpawn.SetActive(true);
            return enemyToSpawn;
        }
    }

    public void ReturnEnemy(string type, GameObject enemy)
    {
        enemy.transform.SetParent(this.transform);
        enemy.SetActive(false);
        pools[type].Acolar(enemy);
    }

    public void LogPoolStatus()
    {
        foreach (var type in pools.Keys)
        {
            int instantiated = totalInstantiated[type];
            int unused = pools[type].Count();
            int used = instantiated - unused;

            Debug.Log($"Estado Pool '{type}': Total instanciados: {instantiated}, Usados: {used}, Sin usar: {unused}");
        }
    }
}

