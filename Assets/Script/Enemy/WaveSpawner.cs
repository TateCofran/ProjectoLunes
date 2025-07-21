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
                    Debug.Log("�Ganaste! Todas las oleadas completadas.");
                    GameManager.Instance.OnVictory();
                }
            }
        }
    }
    
    

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
                countdownText.text = $"Pr�xima oleada en: {Mathf.CeilToInt(timer)}s";
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
}
