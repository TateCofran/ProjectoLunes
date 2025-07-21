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
    public KeyCode alternativeHotkey = KeyCode.Return; 
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
     
        if (!waitingForNextWave && !gridIsExpanding && waveInProgress && currentWave <= maxWaves)
        {
            if (enemiesAlive <= 0)
            {
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

      
        if (waveInProgress && countdownText != null && enableHotkeys)
        {
            string expansionInfo = gridIsExpanding ? " (Expandiendo...)" : "";
            countdownText.text = $"Oleada {currentWave} en progreso{expansionInfo}\n" +
                               $"Enemigos: {enemiesAlive}\n" +
                               $"Presiona {nextWaveHotkey}/{alternativeHotkey} para expandir mapa";
        }

  
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
            yield break;
        }

        Vector2Int coreGrid = new Vector2Int(
            Mathf.RoundToInt(core.transform.position.x / gridManager.cellSize),
            Mathf.RoundToInt(core.transform.position.z / gridManager.cellSize)
        );

       
        Vector3[] fullPath = gridManager.GetPathPositions();
        if (fullPath == null || fullPath.Length == 0)
        {
            Debug.LogError("[WaveSpawner] No se generó ningún camino. Abortando oleada.");
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

        List<Vector2Int> puntosFinales = gridManager.ObtenerPuntosFinales();
        
        if (puntosFinales.Count == 0)
        {
            
            puntosFinales.Add(new Vector2Int(
                Mathf.RoundToInt(spawnPos.x / gridManager.cellSize),
                Mathf.RoundToInt(spawnPos.z / gridManager.cellSize)
            ));
        }


        Vector2Int coreGridPos = new Vector2Int(gridManager.width / 2, 0);

        //puntos de spawn
        int enemiesPerSpawnPoint = Mathf.CeilToInt((float)(totalEnemies - extraEnemies) / puntosFinales.Count);
        int enemiesSpawned = 0;
        int directEnemiesSpawned = 0;
        int normalEnemiesSpawned = 0;

        foreach (var spawnGridPos in puntosFinales)
        {
            int enemiesToSpawnHere = Mathf.Min(enemiesPerSpawnPoint, (totalEnemies - extraEnemies) - enemiesSpawned);
            
            
            for (int i = 0; i < enemiesToSpawnHere; i++)
            {
                Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * gridManager.cellSize, 0, spawnGridPos.y * gridManager.cellSize);

                GameObject go;
                
                // alternar ruta de enemigos
                bool spawnDirectEnemy = (enemiesSpawned % 2 == 0); 
                
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
                    Debug.Log($"[WaveSpawner]  DirectEnemy #{directEnemiesSpawned} (DIJKSTRA - Ruta Corta) spawneado en {spawnGridPos}");
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
                    Debug.Log($"[WaveSpawner]  Enemy Normal #{normalEnemiesSpawned} (BFS - Ruta Larga) spawneado en {spawnGridPos}");
                }

                enemiesSpawned++;
                yield return new WaitForSeconds(1f);
            }
        }

   
        if (isMiniBossWave || isBossWave)
        {
            Vector2Int bossSpawnPoint = puntosFinales[Random.Range(0, puntosFinales.Count)];
            Vector3 bossSpawnPos = new Vector3(bossSpawnPoint.x * gridManager.cellSize, 0, bossSpawnPoint.y * gridManager.cellSize);
        
            if (isMiniBossWave)
            {
                Debug.Log($"[WaveSpawner]  MiniBoss (BFS FORZADO - Camino MÁS largo) spawneando en {bossSpawnPoint}");
                yield return SpawnSpecial("MiniBoss", fullPath, bossSpawnPos);
            }
            else if (isBossWave)
            {
                Debug.Log($"[WaveSpawner]  Boss (BFS FORZADO - Camino MÁS largo) spawneando en {bossSpawnPoint}");
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

       
       
        e.InitializePath(spawnGridPos, coreGridPos, core, this, gridManager);

        yield return new WaitForSeconds(1f);
    }


    
    public void EnemyKilled(DirectEnemy directEnemy)
    {
        enemiesAlive--;
        
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
       
        if (waitingForNextWave && !gridIsExpanding && !isStartingNextWave)
        {
            if (Input.GetKeyDown(nextWaveHotkey) || Input.GetKeyDown(alternativeHotkey))
            {
              
                StartNextWaveViaHotkey();
            }
        }
        
     
        if (Input.GetKeyDown(nextWaveHotkey) || Input.GetKeyDown(alternativeHotkey))
        {
            if (waveInProgress && !gridIsExpanding)
            {
             
                ForceGridExpansion();
            }
        }
        
  
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F1))
        {
            
            ForceGridExpansion();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            
            ForceNextWave();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
          
            KillAllEnemies();
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
        
            StartCoroutine(GenerateMultipleExpansions());
        }
        #endif
    }

    
    private void ForceGridExpansion()
    {
        if (gridIsExpanding)
        {
            return;
        }
        
        StartCoroutine(ForceGridExpansionCoroutine());
    }

    private IEnumerator ForceGridExpansionCoroutine()
    {
        gridIsExpanding = true;
     
        int fakeWaveNumber = currentWave + UnityEngine.Random.Range(1, 4);
       
        gridManager.ApplyRandomValidTileExpansion(fakeWaveNumber);
        
        yield return new WaitForSeconds(0.5f); 
        
        List<Vector2Int> newEndpoints = gridManager.ObtenerPuntosFinales();
        Debug.Log($"[WaveSpawner] Expansión completada. Nuevos puntos de spawn: {newEndpoints.Count}");
        
      
        UpdateEnemyPaths();
        
        gridIsExpanding = false;
    }

  
    private void UpdateEnemyPaths()
    {
        
        Enemy[] activeEnemies = FindObjectsOfType<Enemy>();
        DirectEnemy[] activeDirectEnemies = FindObjectsOfType<DirectEnemy>();
        
        Vector2Int coreGridPos = new Vector2Int(gridManager.width / 2, 0);
        int normalEnemiesUpdated = 0;
        int directEnemiesUpdated = 0;
        
        
        
        foreach (Enemy enemy in activeEnemies)
        {
            if (!enemy.name.Contains("Pool") && enemy.gameObject.activeInHierarchy)
            {
               
                Vector2Int currentGridPos = new Vector2Int(
                    Mathf.RoundToInt(enemy.transform.position.x / gridManager.cellSize),
                    Mathf.RoundToInt(enemy.transform.position.z / gridManager.cellSize)
                );
                
                if (gridManager.gridCells.ContainsKey(currentGridPos))
                {
                    enemy.InitializePath(currentGridPos, coreGridPos, core, this, gridManager);
                    normalEnemiesUpdated++;
                    Debug.Log($"[WaveSpawner]  Enemy Normal ruta actualizada (BFS) desde {currentGridPos}");
                }
            }
        }
        
        foreach (DirectEnemy directEnemy in activeDirectEnemies)
        {
            if (!directEnemy.name.Contains("Pool") && directEnemy.gameObject.activeInHierarchy)
            {
                
                Vector2Int currentGridPos = new Vector2Int(
                    Mathf.RoundToInt(directEnemy.transform.position.x / gridManager.cellSize),
                    Mathf.RoundToInt(directEnemy.transform.position.z / gridManager.cellSize)
                );
                
                if (gridManager.gridCells.ContainsKey(currentGridPos))
                {
                    directEnemy.InitializePathDirect(currentGridPos, coreGridPos, core, this, gridManager);
                    directEnemiesUpdated++;
                    Debug.Log($"[WaveSpawner]  DirectEnemy ruta actualizada (Dijkstra) desde {currentGridPos}");
                }
            }
        }
        
        
        Debug.Log($"  - DirectEnemies actualizados (Dijkstra): {directEnemiesUpdated}");
        Debug.Log($"  - Normal Enemies actualizados (BFS): {normalEnemiesUpdated}");
       
    }

   
    private void StartNextWaveViaHotkey()
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

    
    #if UNITY_EDITOR
    private void ForceNextWave()
    {
        if (waveInProgress)
        {
           
            KillAllEnemies();
        }
        
      
        waitingForNextWave = false;
        gridIsExpanding = false;
        waveInProgress = false;
        isStartingNextWave = false;

        nextWaveButton.gameObject.SetActive(false);
        countdownText.text = "";
        
      
        StartNextWave();
    }

    private void KillAllEnemies()
    {
        
        Enemy[] allEnemies = FindObjectsOfType<Enemy>();
        DirectEnemy[] allDirectEnemies = FindObjectsOfType<DirectEnemy>();
        
        foreach (Enemy enemy in allEnemies)
        {
            if (!enemy.gameObject.name.Contains("Pool")) 
            {
                enemy.GetComponent<Enemy>()?.Die();
            }
        }
        
        foreach (DirectEnemy directEnemy in allDirectEnemies)
        {
            if (!directEnemy.gameObject.name.Contains("Pool"))
            {
                
                directEnemy.SendMessage("Die", SendMessageOptions.DontRequireReceiver);
            }
        }
        
        enemiesAlive = 0;
        
    }

   
    private IEnumerator GenerateMultipleExpansions()
    {
        
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.5f);
            ForceGridExpansion();
            yield return new WaitForSeconds(1f);
        }
        
       
    }
    #endif


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


    public void SetHotkeys(KeyCode primary, KeyCode secondary = KeyCode.None)
    {
        nextWaveHotkey = primary;
        if (secondary != KeyCode.None)
        {
            alternativeHotkey = secondary;
        }
       
    }

   
    public void ToggleHotkeys(bool enabled)
    {
        enableHotkeys = enabled;
      
    }
}