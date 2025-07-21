using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DirectEnemy : MonoBehaviour
{
    private Vector3[] pathPositions;
    private int currentPathIndex;
    public float maxHealth = 5f;
    private float currentHealth;
    public float speed = 2f;
    public float defense = 0f;
    public string enemyType;
    public GameObject healthBarPrefab;
    private Image healthBarFill;
    private GameObject healthBarInstance;
    private bool isDead = false;
    private bool hasHitCore = false;

    private GameObject core;
    private WaveSpawner waveSpawner;
    private GridManager gridManager;

    public void InitializePathDirect(
        Vector2Int spawnGridPos,
        Vector2Int coreGridPos,
        GameObject coreObject,
        WaveSpawner spawner,
        GridManager manager)
    {
        isDead = false;
        core = coreObject;
        waveSpawner = spawner;
        gridManager = manager;
        currentHealth = maxHealth;

        // --- ATENCIÓN: SIEMPRE CALCULA EL CAMINO MÁS CORTO SOBRE EL GRAFO, IGNORANDO EL VISUAL ---
        // Esto NO afecta al SlowEnemy ni al grafo, solo busca el óptimo con Dijkstra

        pathPositions = gridManager.ObtenerCaminoOptimoWorld(spawnGridPos, coreGridPos);

        if (pathPositions == null || pathPositions.Length == 0)
        {
            Debug.LogError("[DirectEnemy] No se pudo generar ruta óptima (Dijkstra).");
            return;
        }

        currentPathIndex = 1;

        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(
                healthBarPrefab,
                transform.position + Vector3.up * 1.5f,
                Quaternion.identity
            );
            healthBarInstance.transform.SetParent(transform);
            healthBarFill = healthBarInstance
                .transform.Find("Background/Filled")
                .GetComponent<Image>();
        }
    }

    void Update()
    {
        if (isDead || pathPositions == null || currentPathIndex >= pathPositions.Length)
            return;

        // Lerp barra de vida
        if (healthBarFill != null)
        {
            float t = Mathf.Clamp01(currentHealth / maxHealth);
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, t, Time.deltaTime * 8f);
        }

        // Movimiento
        Vector3 target = pathPositions[currentPathIndex];
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime * GameSpeedController.SpeedMultiplier
        );
        if (Vector3.Distance(transform.position, target) < 0.1f)
            currentPathIndex++;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        float real = Mathf.Max(0, amount - defense);
        currentHealth -= real;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        if (healthBarInstance != null) Destroy(healthBarInstance);
        EnemyPool.Instance.ReturnEnemy(enemyType, gameObject);
    }
}
