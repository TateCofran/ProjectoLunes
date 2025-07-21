using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance;

    public GameObject slowEnemyPrefab;
    public GameObject fastEnemyPrefab;
    public GameObject miniBossPrefab;
    public GameObject bossPrefab;

    [Header("Hotkey Controls")]
    public KeyCode nextWaveHotkey = KeyCode.Space;
    public KeyCode alternativeHotkey = KeyCode.Return; // Enter
    public bool enableHotkeys = true;
    public GridManager gridManager;
    public GameObject core;

    public int maxWaves = 15;
    public int enemiesPerWave = 3;

    public Button nextWaveButton;
    public TMP_Text countdownText;

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool waitingForNextWave = false;
    private bool gridIsExpanding = false;
    private bool waveInProgress = false;
    private bool isStartingNextWave = false;

    private Coroutine waveRoutine;

    private List<GoldTurret> goldTurrets = new List<GoldTurret>();

    void Start()
    {
        if (nextWaveButton == null || countdownText == null || gridManager == null || core == null)
        {
            Debug.LogError("[WaveSpawner] Un campo no est� asignado en el inspector");
            enabled = false;
            return;
        }
        Instance = this;
        nextWaveButton.onClick.AddListener(StartNextWaveManually);
        countdownText.text = "";
        waveRoutine = StartCoroutine(StartWaveSystem());
    }

    IEnumerator StartWaveSystem()
    {
        yield return new WaitForSeconds(2f);
        StartNextWave();
    }

    void Update()
    {
        // Código existente de Update...
        if (!waitingForNextWave && !gridIsExpanding && waveInProgress && currentWave <= maxWaves)
        {
            if (enemiesAlive <= 0)
            {
                Debug.Log($"Oleada {currentWave} completada");
                EnemyPool.Instance.LogPoolStatus();
                waveInProgress = false;

                GoldManager.Instance.AddGold(15);

                foreach (GoldTurret g in goldTurrets)
                {
                    if (g != null)
                        g.GiveGold();
                }

                if (currentWave < maxWaves)
                {
                    if ((currentWave) % 3 == 0)
                    {
                        GemManager.Instance.AddGemsRun(1);
                    }
                    StartCoroutine(StartWaveDelay());
                }
                else
                {
                    Debug.Log("¡Ganaste! Todas las oleadas completadas.");
                    GameManager.Instance.OnVictory();
                }
            }
        }

        // NUEVO: Sistema de hotkeys
        if (enableHotkeys)
        {
            HandleHotkeyInput();
        }
    }

    
    

    

    public void StartNextWaveManually()
    {
        if (waitingForNextWave && !gridIsExpanding && !isStartingNextWave)
        {
            isStartingNextWave = true;
            waitingForNextWave = false;
            nextWaveButton.gameObject.SetActive(false);
            countdownText.text = "";
            StartNextWave();
        }
    }

    public void StartNextWave()
    {
        if (waveInProgress) return;
        StartCoroutine(WaitForPathAndSpawn());
    }

    
    IEnumerator WaitForPathAndSpawn()
    {
        gridManager.ApplyRandomValidTileExpansion(currentWave + 1);

        yield return null; 
        
        
        
        List<Vector2Int> endpoints = gridManager.ObtenerPuntosFinales();
        if (endpoints == null || endpoints.Count == 0)
        {
            Debug.LogError("[WaveSpawner] No hay puntos finales para spawnear.");
            yield break;
        }

        Vector2Int coreGrid = new Vector2Int(
            Mathf.RoundToInt(core.transform.position.x / gridManager.cellSize),
            Mathf.RoundToInt(core.transform.position.z / gridManager.cellSize)
        );
        Vector3[] fullPath = gridManager.GetPathPositions();
        //Debug.Log("[PATH] --- Puntos del camino:");
        for (int i = 0; i < fullPath.Length; i++)
        {
            //Debug.Log($"[{i}] {fullPath[i]}");
        }
        

        if (fullPath == null || fullPath.Length == 0)
        {
            Debug.LogError("[WaveSpawner] No se gener� ning�n camino. Abortando oleada.");
            yield break;
        }
        if (Vector3.Distance(core.transform.position, fullPath[0]) < 0.5f)
        {
            System.Array.Reverse(fullPath);
        }

        Vector3 spawnPos = fullPath[0];

        currentWave++;
        waveInProgress = true;
        StartCoroutine(SpawnWave(fullPath.ToList(), spawnPos));
    }

    

   IEnumerator SpawnWave(List<Vector3> fullPath, Vector3 spawnPos)
{
    bool isBossWave = currentWave % 15 == 0;
    bool isMiniBossWave = currentWave % 5 == 0 && !isBossWave;

    int extraEnemies = (isBossWave ? 1 : 0) + (isMiniBossWave ? 1 : 0);
    int totalEnemies = enemiesPerWave + (currentWave - 1) * 4 + extraEnemies;
    enemiesAlive = totalEnemies;

    // Obtener TODOS los puntos finales disponibles
    List<Vector2Int> puntosFinales = gridManager.ObtenerPuntosFinales();
    
    Debug.Log($"[WaveSpawner] Spawneando desde {puntosFinales.Count} puntos diferentes");
    
    if (puntosFinales.Count == 0)
    {
        Debug.LogError("No hay puntos finales disponibles!");
        // Usar el punto por defecto
        puntosFinales.Add(new Vector2Int(
            Mathf.RoundToInt(spawnPos.x / gridManager.cellSize),
            Mathf.RoundToInt(spawnPos.z / gridManager.cellSize)
        ));
    }

    Vector2Int coreGridPos = new Vector2Int(gridManager.width / 2, 0);

    // Distribuir enemigos entre todos los puntos de spawn
    int enemiesPerSpawnPoint = Mathf.CeilToInt((float)(totalEnemies - extraEnemies) / puntosFinales.Count);
    int enemiesSpawned = 0;

    foreach (var spawnGridPos in puntosFinales)
    {
        int enemiesToSpawnHere = Mathf.Min(enemiesPerSpawnPoint, (totalEnemies - extraEnemies) - enemiesSpawned);
        
        for (int i = 0; i < enemiesToSpawnHere; i++)
        {
            Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * gridManager.cellSize, 0, spawnGridPos.y * gridManager.cellSize);

            GameObject go;
            if (enemiesSpawned < 2)
            {
                go = EnemyPool.Instance.GetEnemy("Direct");
                var de = go.GetComponent<DirectEnemy>();
                go.transform.position = spawnWorldPos;
                de.enemyType = "Direct";
                de.InitializePathDirect(
                    spawnGridPos,
                    coreGridPos,
                    core,
                    this,
                    gridManager
                );
            }
            else
            {
                go = EnemyPool.Instance.GetEnemy("Slow");
                var e = go.GetComponent<Enemy>();
                go.transform.position = spawnWorldPos;
                e.enemyType = "Slow";
                e.InitializePath(spawnGridPos, coreGridPos, core, this, gridManager);
            }

            
            enemiesSpawned++;
            Debug.Log($"[WaveSpawner] Enemigo {enemiesSpawned}/{totalEnemies} spawneado en {spawnGridPos}");
            
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Spawn de boss/miniboss en un punto aleatorio
    if (isMiniBossWave || isBossWave)
    {
        Vector2Int bossSpawnPoint = puntosFinales[Random.Range(0, puntosFinales.Count)];
        Vector3 bossSpawnPos = new Vector3(bossSpawnPoint.x * gridManager.cellSize, 0, bossSpawnPoint.y * gridManager.cellSize);
        
        if (isMiniBossWave)
            yield return SpawnSpecial("MiniBoss", fullPath, bossSpawnPos);
        else if (isBossWave)
            yield return SpawnSpecial("Boss", fullPath, bossSpawnPos);
    }
}

    IEnumerator SpawnSpecial(string type, List<Vector3> fullPath, Vector3 spawnPos)
    {
        var go = EnemyPool.Instance.GetEnemy(type);
        go.transform.position = spawnPos;
        var e = go.GetComponent<Enemy>();
        e.enemyType = type;

        Vector2Int spawnGridPos = new Vector2Int(
            Mathf.RoundToInt(spawnPos.x / gridManager.cellSize),
            Mathf.RoundToInt(spawnPos.z / gridManager.cellSize)
        );

        Vector3 coreWorldPos = fullPath[0];
        Vector2Int coreGridPos = new Vector2Int(
            Mathf.RoundToInt(coreWorldPos.x / gridManager.cellSize),
            Mathf.RoundToInt(coreWorldPos.z / gridManager.cellSize)
        );

        e.InitializePath(spawnGridPos, coreGridPos, core, this, gridManager);

        yield return new WaitForSeconds(1f);
    }


    public void EnemyKilled(Enemy enemy)
    {
        enemiesAlive--;
    }

    public void RegisterGoldTurret(GoldTurret turret)
    {
        if (!goldTurrets.Contains(turret))
            goldTurrets.Add(turret);
    }
    
    private void HandleHotkeyInput()
{
    // Detectar hotkeys solo si estamos esperando la siguiente oleada
    if (waitingForNextWave && !gridIsExpanding && !isStartingNextWave)
    {
        if (Input.GetKeyDown(nextWaveHotkey) || Input.GetKeyDown(alternativeHotkey))
        {
            Debug.Log($"[WaveSpawner] Hotkey detectado: {(Input.GetKeyDown(nextWaveHotkey) ? nextWaveHotkey.ToString() : alternativeHotkey.ToString())}");
            StartNextWaveViaHotkey();
        }
    }
    
    // Hotkey de emergencia para forzar oleada (solo en desarrollo)
    #if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.F1))
    {
        Debug.Log("[WaveSpawner] Hotkey F1: Forzando siguiente oleada (modo desarrollo)");
        ForceNextWave();
    }
    
    if (Input.GetKeyDown(KeyCode.F2))
    {
        Debug.Log("[WaveSpawner] Hotkey F2: Eliminando todos los enemigos");
        KillAllEnemies();
    }
    #endif
}

// NUEVA FUNCIÓN: Iniciar oleada vía hotkey
private void StartNextWaveViaHotkey()
{
    if (waitingForNextWave && !gridIsExpanding && !isStartingNextWave)
    {
        isStartingNextWave = true;
        waitingForNextWave = false;
        nextWaveButton.gameObject.SetActive(false);
        countdownText.text = "";
        
        Debug.Log($"[WaveSpawner] Iniciando oleada {currentWave + 1} vía hotkey");
        StartNextWave();
    }
}

// NUEVA FUNCIÓN: Forzar oleada (solo desarrollo)
#if UNITY_EDITOR
private void ForceNextWave()
{
    if (waveInProgress)
    {
        // Si hay oleada en progreso, eliminar todos los enemigos primero
        KillAllEnemies();
    }
    
    // Resetear estados
    waitingForNextWave = false;
    gridIsExpanding = false;
    waveInProgress = false;
    isStartingNextWave = false;
    
    // Ocultar UI
    nextWaveButton.gameObject.SetActive(false);
    countdownText.text = "";
    
    // Iniciar siguiente oleada
    StartNextWave();
}

private void KillAllEnemies()
{
    // Encontrar todos los enemigos activos y eliminarlos
    Enemy[] allEnemies = FindObjectsOfType<Enemy>();
    DirectEnemy[] allDirectEnemies = FindObjectsOfType<DirectEnemy>();
    
    foreach (Enemy enemy in allEnemies)
    {
        if (!enemy.gameObject.name.Contains("Pool")) // No afectar enemigos en pool
        {
            enemy.GetComponent<Enemy>()?.Die();
        }
    }
    
    foreach (DirectEnemy directEnemy in allDirectEnemies)
    {
        if (!directEnemy.gameObject.name.Contains("Pool"))
        {
            // DirectEnemy no tiene método Die público, usar el private
            directEnemy.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
        }
    }
    
    enemiesAlive = 0;
    Debug.Log("[WaveSpawner] Todos los enemigos eliminados forzadamente");
}
#endif

// Modificar StartWaveDelay para mostrar información del hotkey
IEnumerator StartWaveDelay()
{
    float timer = 30f;
    waitingForNextWave = true;
    isStartingNextWave = false;
    nextWaveButton.gameObject.SetActive(true);
    nextWaveButton.interactable = false;

    gridIsExpanding = false;

    nextWaveButton.interactable = true;
    countdownText.text = "";

    while (waitingForNextWave && timer > 0)
    {
        timer -= Time.deltaTime;
        if (countdownText != null)
        {
            // Mostrar información del hotkey en el texto del countdown
            string hotkeyInfo = enableHotkeys ? $"\nPresiona {nextWaveHotkey} o {alternativeHotkey} para avanzar" : "";
            countdownText.text = $"Próxima oleada en: {Mathf.CeilToInt(timer)}s{hotkeyInfo}";
        }
        yield return null;
    }

    if (!isStartingNextWave)
    {
        waitingForNextWave = false;
        nextWaveButton.gameObject.SetActive(false);
        countdownText.text = "";
        StartNextWave();
    }
}

// NUEVA FUNCIÓN: Configurar hotkeys desde inspector o código
public void SetHotkeys(KeyCode primary, KeyCode secondary = KeyCode.None)
{
    nextWaveHotkey = primary;
    if (secondary != KeyCode.None)
    {
        alternativeHotkey = secondary;
    }
    Debug.Log($"[WaveSpawner] Hotkeys configurados: {primary}" + (secondary != KeyCode.None ? $" y {secondary}" : ""));
}

// NUEVA FUNCIÓN: Toggle hotkeys
public void ToggleHotkeys(bool enabled)
{
    enableHotkeys = enabled;
    Debug.Log($"[WaveSpawner] Hotkeys {(enabled ? "habilitados" : "deshabilitados")}");
}
}
