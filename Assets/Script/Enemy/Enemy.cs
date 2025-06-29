using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class Enemy : MonoBehaviour
{
    private Vector3[] pathPositions;
    private GameObject core;
    private WaveSpawner waveSpawner;
    private GridManager gridManager;

    private Vector3[] mainPathPositions;
    private int savedMainPathIndex;

    private int currentPathIndex = 0;
    private List<Vector3> debugPath = new List<Vector3>();

    // uso PilaTF en vez de Stack
    private PilaTF<(Vector3[] path, int index)> previousPaths = new PilaTF<(Vector3[], int)>();

    private HashSet<PathConnection> usedConnections = new HashSet<PathConnection>();

    [Header("Enemy Stats")]
    public float maxHealth = 5f;
    public float currentHealth;
    public float speed = 2f;
    public float defense = 0f;

    private bool isDead = false;
    public string enemyType;

    [Header("Health UI")]
    public GameObject healthBarPrefab;
    private Image healthBarFill;
    private GameObject healthBarInstance;
    List<Vector3> fullPathDebug = new List<Vector3>();

    private float originalSpeed;
    private Coroutine slowCoroutine;

    public void InitializePath(Vector2Int spawnGridPos, Vector2Int coreGridPos, GameObject coreObject, WaveSpawner spawner, GridManager manager)
    {
        isDead = false;
        previousPaths.InicializarPila();

        core = coreObject;
        waveSpawner = spawner;
        gridManager = manager;
        currentHealth = maxHealth;

        // --- Pedir camino óptimo ---
        pathPositions = gridManager.ObtenerCaminoOptimoWorld(spawnGridPos, coreGridPos);
        Debug.Log($"[ENEMY] pathPositions: {string.Join(" -> ", pathPositions.Select(p => p.ToString()))}");

        mainPathPositions = pathPositions;
        currentPathIndex = 1; // Empieza en el primer segmento (del 0 al 1)

        if (pathPositions == null || pathPositions.Length == 0)
        {
            Debug.LogError("pathPositions está vacío o NULL.");
            return;
        }

        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            healthBarInstance.transform.SetParent(transform);
            healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }
    }

    void Update()
    {
        if (isDead || currentPathIndex >= pathPositions.Length) return;

        if (healthBarFill != null)
        {
            float targetFill = Mathf.Clamp01(currentHealth / maxHealth);
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetFill, Time.deltaTime * 8f);
        }

        //Debug.Log($"[ENEMY MOVE] {name} en {transform.position}, yendo a [{currentPathIndex}] {pathPositions[currentPathIndex]}");
        Move();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        float realDamage = Mathf.Max(0, amount - defense);
        currentHealth -= realDamage;

        if (healthBarFill != null)
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (healthBarInstance != null)
            Destroy(healthBarInstance);

        if (WaveSpawner.Instance != null)
            WaveSpawner.Instance.EnemyKilled(this);

        EnemyPool.Instance.ReturnEnemy(enemyType, gameObject);
    }

    public void Move()
    {
        if (pathPositions == null || currentPathIndex >= pathPositions.Length) return;

        Vector3 target = pathPositions[currentPathIndex];

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime * GameSpeedController.SpeedMultiplier);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            AdvanceToNextSegment();
        }
    }


    private void AdvanceToNextSegment()
    {
        if (currentPathIndex < pathPositions.Length - 1)
        {
            currentPathIndex++;
        }
    }




    private void DebugDrawPath()
    {
        if (debugPath.Count > 1)
        {
            for (int i = 0; i < debugPath.Count - 1; i++)
            {
                Debug.DrawLine(debugPath[i], debugPath[i + 1], Color.magenta);
            }

            Debug.DrawLine(transform.position, pathPositions[currentPathIndex], Color.green);
            Debug.DrawRay(transform.position + Vector3.up * 0.5f, (pathPositions[currentPathIndex] - transform.position), Color.red);
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public float GetSpeed()
    {
        return speed;
    }
    public void MultiplySpeed(float multiplier)
    {
        speed *= multiplier;
    }

    public bool HasReachedEnd()
    {
        return currentPathIndex >= pathPositions.Length;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Core"))
        {
            Debug.Log("Enemy collided with Core. Triggering damage and death.");
            core.GetComponent<Core>().TakeDamage(1);
            Die();
        }
    }

    int FindNextForwardIndex(Vector3[] newPath, Vector3 currentPosition)
    {
        for (int i = 0; i < newPath.Length; i++)
        {
            if (Vector3.Distance(newPath[i], currentPosition) < 0.5f)
                return i;
        }
        return -1;
    }

    void OnDrawGizmosSelected()
    {
        if (pathPositions != null && pathPositions.Length > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < pathPositions.Length - 1; i++)
            {
                Gizmos.DrawLine(pathPositions[i] + Vector3.up * 0.3f, pathPositions[i + 1] + Vector3.up * 0.3f);
            }
        }
    }

}

