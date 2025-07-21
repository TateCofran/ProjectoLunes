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
            Debug.LogError("[WaveSpawner] Un campo no está asignado en el inspector");
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

        // Actualizar UI durante oleada
        if (waveInProgress && countdownText != null && enableHotkeys)
        {
            string expansionInfo = gridIsExpanding ? " (Expandiendo...)" : "";
            countdownText.text = $"Oleada {currentWave} en progreso{expansionInfo}\n" +
                               $"Enemigos: {enemiesAlive}\n" +
                               $"Presiona {nextWaveHotkey}/{alternativeHotkey} para expandir mapa";
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
        // IMPORTANTE: Pasar currentWave + 1 para que la primera oleada sea 1, no 0
        Debug.Log($"[WaveSpawner] Expandiendo grid para oleada {currentWave + 1}");
        gridManager.ApplyRandomValidTileExpansion(currentWave + 1);

        yield return null; 

        // Obtener puntos finales después de la expansión
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

        // Verificar que tenemos un camino válido
        Vector3[] fullPath = gridManager.GetPathPositions();
        if (fullPath == null || fullPath.Length == 0)
        {
            Debug.LogError("[WaveSpawner] No se generó ningún camino. Abortando oleada.");
            yield break;
        }

        // Asegurar que el path va desde los extremos hacia el core
        if (Vector3.Distance(core.transform.position, fullPath[0]) < 0.5f)
        {
            System.Array.Reverse(fullPath);
        }

        Vector3 spawnPos = fullPath[0];

        currentWave++;
        waveInProgress = true;
        
        // Log para debugging
        Debug.Log($"[WaveSpawner] Iniciando oleada {currentWave}");
        Debug.Log($"[WaveSpawner] Puntos de spawn disponibles: {endpoints.Count}");
        foreach (var endpoint in endpoints)
        {
            Debug.Log($"  - Spawn point: {endpoint}");
        }
        
        StartCoroutine(SpawnWave(fullPath.ToList(), spawnPos));
    }

 
    IEnumerator SpawnWave(List<Vector3> fullPath, Vector3 spawnPos)
    {
        bool isBossWave = currentWave % 15 == 0;
        bool isMiniBossWave = currentWave % 5 == 0 && !isBossWave;

        int extraEnemies = (isBossWave ? 1 : 0) + (isMiniBossWave ? 1 : 0);
        int totalEnemies = enemiesPerWave + (currentWave - 1) * 4 + extraEnemies;
        enemiesAlive = totalEnemies;

        List<Vector2Int> puntosFinales = gridManager.ObtenerPuntosFinales();
        
        if (puntosFinales.Count == 0)
        {
            Debug.LogError("No hay puntos finales disponibles!");
            puntosFinales.Add(new Vector2Int(
                Mathf.RoundToInt(spawnPos.x / gridManager.cellSize),
                Mathf.RoundToInt(spawnPos.z / gridManager.cellSize)
            ));
        }


        Vector2Int coreGridPos = new Vector2Int(gridManager.width / 2, 0);

        // Distribuir enemigos entre todos los puntos de spawn
        int enemiesPerSpawnPoint = Mathf.CeilToInt((float)(totalEnemies - extraEnemies) / puntosFinales.Count);
        int enemiesSpawned = 0;
        int directEnemiesSpawned = 0;
        int normalEnemiesSpawned = 0;

        foreach (var spawnGridPos in puntosFinales)
        {
            int enemiesToSpawnHere = Mathf.Min(enemiesPerSpawnPoint, (totalEnemies - extraEnemies) - enemiesSpawned);
            
            Debug.Log($"[WaveSpawner] Spawneando {enemiesToSpawnHere} enemigos en punto {spawnGridPos}");
            
            for (int i = 0; i < enemiesToSpawnHere; i++)
            {
                Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * gridManager.cellSize, 0, spawnGridPos.y * gridManager.cellSize);

                GameObject go;
                
                // Estrategia: Alternar entre DirectEnemy y Enemy normal para comparación directa
                bool spawnDirectEnemy = (enemiesSpawned % 2 == 0); // Alternar cada enemigo
                
                if (spawnDirectEnemy && directEnemiesSpawned < (totalEnemies - extraEnemies) / 2)
                {
                    // DirectEnemy - usa Dijkstra (ruta óptima)
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
                    directEnemiesSpawned++;
                    Debug.Log($"[WaveSpawner] 🔵 DirectEnemy #{directEnemiesSpawned} (DIJKSTRA - Ruta Corta) spawneado en {spawnGridPos}");
                }
                else
                {
                    // Enemy Normal - usa BFS (camino más largo)
                    go = EnemyPool.Instance.GetEnemy("Slow");
                    var e = go.GetComponent<Enemy>();
                    go.transform.position = spawnWorldPos;
                    e.enemyType = "Slow";
                    e.InitializePath(spawnGridPos, coreGridPos, core, this, gridManager);
                    normalEnemiesSpawned++;
                    Debug.Log($"[WaveSpawner] 🔴 Enemy Normal #{normalEnemiesSpawned} (BFS - Ruta Larga) spawneado en {spawnGridPos}");
                }

                enemiesSpawned++;
                yield return new WaitForSeconds(1f); // Aumentar delay para mejor observación de las diferencias
            }
        }

        // Spawn de boss/miniboss en un punto aleatorio
        if (isMiniBossWave || isBossWave)
        {
            Vector2Int bossSpawnPoint = puntosFinales[Random.Range(0, puntosFinales.Count)];
            Vector3 bossSpawnPos = new Vector3(bossSpawnPoint.x * gridManager.cellSize, 0, bossSpawnPoint.y * gridManager.cellSize);
        
            if (isMiniBossWave)
            {
                Debug.Log($"[WaveSpawner] 🟠 MiniBoss (BFS FORZADO - Camino MÁS largo) spawneando en {bossSpawnPoint}");
                yield return SpawnSpecial("MiniBoss", fullPath, bossSpawnPos);
            }
            else if (isBossWave)
            {
                Debug.Log($"[WaveSpawner] 🟠 Boss (BFS FORZADO - Camino MÁS largo) spawneando en {bossSpawnPoint}");
                yield return SpawnSpecial("Boss", fullPath, bossSpawnPos);
            }
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

       
        Debug.Log($"[WaveSpawner] 🟠 {type} FORZADO a usar BFS (camino largo) desde {spawnGridPos}");
        e.InitializePath(spawnGridPos, coreGridPos, core, this, gridManager); // NUEVO MÉTODO

        yield return new WaitForSeconds(1f);
    }


    
    public void EnemyKilled(DirectEnemy directEnemy)
    {
        enemiesAlive--;
        Debug.Log($"[WaveSpawner] 🔵 DirectEnemy (Dijkstra) eliminado. Enemigos restantes: {enemiesAlive}");
    }

    public void EnemyKilled(Enemy enemy)
    {
        enemiesAlive--;
        Debug.Log($"[WaveSpawner] 🔴 Enemy Normal (BFS) eliminado. Enemigos restantes: {enemiesAlive}");
    }

    public void RegisterGoldTurret(GoldTurret turret)
    {
        if (!goldTurrets.Contains(turret))
            goldTurrets.Add(turret);
    }
    
    // ACTUALIZADO: HandleHotkeyInput para expansión durante oleada
    private void HandleHotkeyInput()
    {
        // Hotkey para avanzar oleada cuando está esperando
        if (waitingForNextWave && !gridIsExpanding && !isStartingNextWave)
        {
            if (Input.GetKeyDown(nextWaveHotkey) || Input.GetKeyDown(alternativeHotkey))
            {
                Debug.Log($"[WaveSpawner] Hotkey detectado: Iniciando oleada {currentWave + 1}");
                StartNextWaveViaHotkey();
            }
        }
        
        // NUEVO: Hotkey para forzar expansión del grid DURANTE la oleada
        if (Input.GetKeyDown(nextWaveHotkey) || Input.GetKeyDown(alternativeHotkey))
        {
            if (waveInProgress && !gridIsExpanding)
            {
                Debug.Log($"[WaveSpawner] Hotkey durante oleada: Forzando expansión del mapa");
                ForceGridExpansion();
            }
        }
        
        // Hotkeys de desarrollo
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[WaveSpawner] F1: Forzando expansión de grid");
            ForceGridExpansion();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[WaveSpawner] F2: Forzando siguiente oleada completa");
            ForceNextWave();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("[WaveSpawner] F3: Eliminando todos los enemigos");
            KillAllEnemies();
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("[WaveSpawner] F4: Generando múltiples expansiones");
            StartCoroutine(GenerateMultipleExpansions());
        }
        #endif
    }

    // NUEVA FUNCIÓN: Forzar expansión del grid durante oleada activa
    private void ForceGridExpansion()
    {
        if (gridIsExpanding)
        {
            Debug.LogWarning("[WaveSpawner] Ya hay una expansión en progreso");
            return;
        }
        
        StartCoroutine(ForceGridExpansionCoroutine());
    }

    private IEnumerator ForceGridExpansionCoroutine()
    {
        gridIsExpanding = true;
        
        Debug.Log($"[WaveSpawner] Forzando expansión del grid durante oleada {currentWave}");
        
        // Incrementar un número artificial para la expansión
        int fakeWaveNumber = currentWave + UnityEngine.Random.Range(1, 4);
        
        // Aplicar expansión
        gridManager.ApplyRandomValidTileExpansion(fakeWaveNumber);
        
        yield return new WaitForSeconds(0.5f); // Pequeña pausa para ver el cambio
        
        // Obtener nuevos puntos de spawn
        List<Vector2Int> newEndpoints = gridManager.ObtenerPuntosFinales();
        Debug.Log($"[WaveSpawner] Expansión completada. Nuevos puntos de spawn: {newEndpoints.Count}");
        
        // Si hay enemigos vivos, pueden usar los nuevos caminos en su próximo recálculo
        UpdateEnemyPaths();
        
        gridIsExpanding = false;
    }

    // NUEVA FUNCIÓN: Actualizar caminos de enemigos existentes
    private void UpdateEnemyPaths()
    {
        // Encontrar todos los enemigos activos
        Enemy[] activeEnemies = FindObjectsOfType<Enemy>();
        DirectEnemy[] activeDirectEnemies = FindObjectsOfType<DirectEnemy>();
        
        Vector2Int coreGridPos = new Vector2Int(gridManager.width / 2, 0);
        int normalEnemiesUpdated = 0;
        int directEnemiesUpdated = 0;
        
        Debug.Log("[WaveSpawner] Actualizando rutas de enemigos existentes...");
        
        foreach (Enemy enemy in activeEnemies)
        {
            if (!enemy.name.Contains("Pool") && enemy.gameObject.activeInHierarchy)
            {
                // Recalcular ruta desde posición actual usando BFS
                Vector2Int currentGridPos = new Vector2Int(
                    Mathf.RoundToInt(enemy.transform.position.x / gridManager.cellSize),
                    Mathf.RoundToInt(enemy.transform.position.z / gridManager.cellSize)
                );
                
                if (gridManager.gridCells.ContainsKey(currentGridPos))
                {
                    enemy.InitializePath(currentGridPos, coreGridPos, core, this, gridManager);
                    normalEnemiesUpdated++;
                    Debug.Log($"[WaveSpawner] 🔴 Enemy Normal ruta actualizada (BFS) desde {currentGridPos}");
                }
            }
        }
        
        foreach (DirectEnemy directEnemy in activeDirectEnemies)
        {
            if (!directEnemy.name.Contains("Pool") && directEnemy.gameObject.activeInHierarchy)
            {
                // Recalcular ruta desde posición actual usando Dijkstra
                Vector2Int currentGridPos = new Vector2Int(
                    Mathf.RoundToInt(directEnemy.transform.position.x / gridManager.cellSize),
                    Mathf.RoundToInt(directEnemy.transform.position.z / gridManager.cellSize)
                );
                
                if (gridManager.gridCells.ContainsKey(currentGridPos))
                {
                    directEnemy.InitializePathDirect(currentGridPos, coreGridPos, core, this, gridManager);
                    directEnemiesUpdated++;
                    Debug.Log($"[WaveSpawner] 🔵 DirectEnemy ruta actualizada (Dijkstra) desde {currentGridPos}");
                }
            }
        }
        
        Debug.Log($"[WaveSpawner] Actualización completada:");
        Debug.Log($"  - DirectEnemies actualizados (Dijkstra): {directEnemiesUpdated}");
        Debug.Log($"  - Normal Enemies actualizados (BFS): {normalEnemiesUpdated}");
        Debug.Log($"[WaveSpawner] ¡Observa cómo los azules toman nuevos atajos y los rojos dan vueltas!");
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

    // NUEVA FUNCIÓN: Generar múltiples expansiones rápidas (para testing)
    private IEnumerator GenerateMultipleExpansions()
    {
        Debug.Log("[WaveSpawner] Generando múltiples expansiones para testing...");
        
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.5f);
            ForceGridExpansion();
            yield return new WaitForSeconds(1f); // Esperar a que termine la expansión
        }
        
        Debug.Log("[WaveSpawner] Múltiples expansiones completadas");
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