using System.Collections;
using System.Collections.Generic;
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

    // CAMBIO: uso PilaTF en vez de Stack
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

    public void InitializePath(Vector3[] positions, GameObject coreObject, WaveSpawner spawner, GridManager manager)
    {
        isDead = false;

        //Debug.Log($"[ENEMY INIT] Posición al inicializar: {transform.position}");

        previousPaths.InicializarPila(); // INICIALIZAR PILA

        pathPositions = positions;
        mainPathPositions = positions;
        currentPathIndex = positions.Length - 1;

        if (pathPositions == null || pathPositions.Length == 0)
        {
            Debug.LogError("pathPositions está vacío o NULL.");
            return;
        }

        core = coreObject;
        waveSpawner = spawner;
        gridManager = manager;
        currentHealth = maxHealth;

        // NO cambies la posición acá. Ya se hizo en el WaveSpawner.
        // transform.position = pathPositions[0];   <--- QUITAR ESTA LÍNEA

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
        if (pathPositions == null || currentPathIndex <= 0) return;

        Vector3 target = pathPositions[currentPathIndex - 1];

        //Debug.Log($"[ENEMY MOVE] {gameObject.name} en {transform.position}, yendo a [{currentPathIndex - 1}] {target}");

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            AdvanceToNextSegment();
        }

        DebugDrawPath();
    }

    private void AdvanceToNextSegment()
    {
        if (currentPathIndex > 0)
        {
            currentPathIndex--;
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
}

