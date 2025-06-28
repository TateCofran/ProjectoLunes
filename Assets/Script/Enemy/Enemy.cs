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
        mainPathPositions = positions;
        pathPositions = positions;
        currentPathIndex = 0;

        previousPaths.InicializarPila(); // INICIALIZAR PILA

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


    void Start()
    {
        if (pathPositions != null && pathPositions.Length > 0)
        {
            fullPathDebug = new List<Vector3>(pathPositions);
        }
        if (pathPositions.Length > 1 && Vector3.Distance(transform.position, pathPositions[0]) < 0.1f)
        {
            currentPathIndex = 1;
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

        Vector3 targetPosition = currentPathIndex < pathPositions.Length
            ? pathPositions[currentPathIndex]
            : pathPositions[pathPositions.Length - 1];

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * GameSpeedController.SpeedMultiplier * Time.deltaTime);

        if (fullPathDebug.Count > 1)
        {
            for (int i = 0; i < fullPathDebug.Count - 1; i++)
            {
                Debug.DrawLine(fullPathDebug[i], fullPathDebug[i + 1], Color.magenta);
            }
        }

        Collider[] hitCurrent = Physics.OverlapSphere(transform.position, 0.1f);
        foreach (var col in hitCurrent)
        {
            if (col.TryGetComponent<PathConnection>(out var conn))
            {
                Debug.DrawRay(transform.position + Vector3.up * 0.5f, Vector3.up, Color.yellow);
                Debug.Log($"Nodo actual tiene PathConnection: de {conn.fromPath} a {conn.toPath}");

                if (Random.value <= conn.probabilityToChooseSecondary)
                {
                    Vector3[] newPath = gridManager.GetPathPositions(conn.toPath);

                    int entryIndex = -1;
                    float minDist = float.MaxValue;
                    for (int i = 0; i < newPath.Length; i++)
                    {
                        float dist = Vector3.Distance(transform.position, newPath[i]);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            entryIndex = i;
                        }
                    }

                    if (entryIndex != -1)
                    {
                        List<Vector3> newFull = new List<Vector3>(fullPathDebug);
                        for (int i = entryIndex; i < newPath.Length; i++)
                            newFull.Add(newPath[i]);

                        // GUARDAR el camino anterior en la PILA antes de cambiar
                        previousPaths.Apilar((pathPositions, currentPathIndex));

                        pathPositions = newPath;
                        currentPathIndex = Mathf.Max(0, entryIndex + 1);

                        fullPathDebug = newFull;

                        Debug.Log($"Enemy switched path to {conn.toPath} at segment {currentPathIndex}");
                    }

                    break;
                }
            }
        }

        if (currentPathIndex < pathPositions.Length &&
            Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            HandleNextSegment();
        }
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

    void HandleNextSegment()
    {
        if (currentPathIndex > 0)
        {
            currentPathIndex--;
        }
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

    public void ApplySlow(float amount, float duration)
    {
        if (slowCoroutine != null)
            StopCoroutine(slowCoroutine);

        slowCoroutine = StartCoroutine(SlowEffect(amount, duration));
    }

    private IEnumerator SlowEffect(float amount, float duration)
    {
        float original = speed;
        speed *= (1f - amount);

        yield return new WaitForSeconds(duration);

        speed = original;
        slowCoroutine = null;
    }
}

