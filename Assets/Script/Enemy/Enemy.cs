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
    private bool hasHitCore = false;

    public void InitializePath(Vector2Int spawnGridPos, Vector2Int coreGridPos, GameObject coreObject, WaveSpawner spawner, GridManager manager)
    {
        isDead = false;
        previousPaths.InicializarPila();

        core = coreObject;
        waveSpawner = spawner;
        gridManager = manager;
        currentHealth = maxHealth;

     
        pathPositions = gridManager.ObtenerCaminoNormalWorld(spawnGridPos, coreGridPos);
        Debug.Log($"[ENEMY NORMAL - BFS] pathPositions: {string.Join(" -> ", pathPositions?.Select(p => p.ToString()) ?? new string[0])}");

        mainPathPositions = pathPositions;
        currentPathIndex = 1;

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
            int goldPerKill = GameStats.GoldPerKill;
            GoldManager.Instance.AddGold(goldPerKill);
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


  /*  public void InitializePathBFS(Vector2Int spawnGridPos, Vector2Int coreGridPos, GameObject coreObject, WaveSpawner spawner, GridManager manager)
    {
        isDead = false;
        previousPaths.InicializarPila();

        core = coreObject;
        waveSpawner = spawner;
        gridManager = manager;
        currentHealth = maxHealth;

        // FORZAR BFS explícitamente para boss/mini-boss (nunca usar Dijkstra como fallback)
        Debug.Log($"[BOSS/MINI-BOSS - BFS FORZADO] Calculando camino MÁS largo desde {spawnGridPos} a {coreGridPos}");
    
        pathPositions = gridManager.ObtenerCaminoNormalWorld(spawnGridPos, coreGridPos);
    
        if (pathPositions == null || pathPositions.Length == 0)
        {
            Debug.LogError("[BOSS/MINI-BOSS - BFS FORZADO] ¡ERROR! No se pudo generar camino BFS para boss.");
        
            // Para boss, si BFS falla, crear un camino directo simple pero NO usar Dijkstra
            Vector3 start = new Vector3(spawnGridPos.x * gridManager.cellSize, 0, spawnGridPos.y * gridManager.cellSize);
            Vector3 end = new Vector3(coreGridPos.x * gridManager.cellSize, 0, coreGridPos.y * gridManager.cellSize);
            pathPositions = new Vector3[] { start, end };
        
            Debug.LogWarning("[BOSS/MINI-BOSS - BFS FORZADO] Usando camino directo de emergencia (NO Dijkstra)");
        }
        else
        {
            Debug.Log($"[BOSS/MINI-BOSS - BFS FORZADO] ✅ Camino largo generado: {pathPositions.Length} puntos");
            Debug.Log($"[BOSS/MINI-BOSS - BFS FORZADO] Esto debería ser MÁS largo que el camino de DirectEnemy");
        }

        mainPathPositions = pathPositions;
        currentPathIndex = 1;

        if (healthBarPrefab != null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            healthBarInstance.transform.SetParent(transform);
            healthBarFill = healthBarInstance.transform.Find("Background/Filled").GetComponent<Image>();
        }
    } */


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
        if (other.CompareTag("Core") && !hasHitCore)
        {
            hasHitCore = true; 
            Debug.Log("Enemy collided with Core. Triggering damage and death.");
            other.GetComponent<Core>().TakeDamage(1);
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
            Vector3 from = pathPositions[i] + Vector3.up * 0.4f;
            Vector3 to = pathPositions[i + 1] + Vector3.up * 0.4f;
            
            
            for (float offset = -0.02f; offset <= 0.02f; offset += 0.01f)
            {
                Gizmos.DrawLine(from + Vector3.right * offset, to + Vector3.right * offset);
            }
        }
        
      
        if (currentPathIndex < pathPositions.Length)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pathPositions[currentPathIndex] + Vector3.up * 0.4f, 0.3f);
        }
        
     
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(pathPositions[0] + Vector3.up * 0.4f, 0.25f);
        
      
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(pathPositions[pathPositions.Length - 1] + Vector3.up * 0.4f, 0.25f);
    }
}
void OnDrawGizmos()
{
    if (!isDead && pathPositions != null && pathPositions.Length > 0)
    {
       
        if (currentPathIndex < pathPositions.Length)
        {
            Gizmos.color = Color.red;
            Vector3 start = transform.position;
            Vector3 end = pathPositions[currentPathIndex];
            
            Gizmos.DrawLine(start, end);
            
         
            Gizmos.DrawLine(start + Vector3.right * 0.1f, end + Vector3.right * 0.1f);
            Gizmos.DrawLine(start - Vector3.right * 0.1f, end - Vector3.right * 0.1f);
            
            
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Enemy Normal (BFS)\n" +
                $"Camino Largo/Ineficiente\n" +
                $"Evita Atajos\n" +
                $"Next: {currentPathIndex}/{pathPositions.Length}\n" +
                $"Ruta: {pathPositions.Length} puntos");
            #endif
        }
        
       
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.8f, 0.2f);
    }
}

}

