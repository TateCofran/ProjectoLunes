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

    public int maxWaves = 45;
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
                        gridIsExpanding = true;
                        //gridManager.ExpandGrid(5, 5);
                    }
                    StartCoroutine(StartWaveDelay());
                }
                else
                {
                    Debug.Log("¡Ganaste! Todas las oleadas completadas.");
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
                countdownText.text = $"Próxima oleada en: {Mathf.CeilToInt(timer)}s";
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
        // 1. Expande el mapa antes de la oleada
        gridManager.AgregarTile();
        yield return null; // O esperar a que termine de instanciar (por si es async)

        // 2. Ahora sí, obtenés el path del nuevo tile
        int lastPathIndex = gridManager.GetTotalPaths() - 1;
        Vector3[] pathPositions = gridManager.GetPathPositions(lastPathIndex);

        if (pathPositions == null || pathPositions.Length == 0)
        {
            Debug.LogError("[WaveSpawner] No se generó ningún camino. Abortando oleada.");
            yield break;
        }

        currentWave++;
        waveInProgress = true;
        // Ahora podés spawnear los enemigos correctamente sobre el nuevo tile/path
        StartCoroutine(SpawnWave());
    }


    public void EnemyKilled(Enemy enemy)
    {
        enemiesAlive--;
        //Debug.Log($"Enemy eliminado. Enemigos vivos restantes: {enemiesAlive}");

    }

    IEnumerator SpawnWave()
    {
        bool isBossWave = currentWave % 15 == 0;
        bool isMiniBossWave = currentWave % 5 == 0 && !isBossWave;

        int extraEnemies = 0;
        if (isMiniBossWave) extraEnemies++;
        if (isBossWave) extraEnemies++;

        int totalEnemies = enemiesPerWave + (currentWave - 1) * 4 + extraEnemies;
        enemiesAlive = totalEnemies;

        Debug.Log($"Oleada {currentWave} iniciada. Spawneando {enemiesAlive} enemigos.");

        waveInProgress = true;

        Vector3[] fullPath = gridManager.GetFullPathPositionsForward();
        Vector3 spawnPos = gridManager.GetLastTileEntryWorldPos();

        for (int i = 0; i < totalEnemies - extraEnemies; i++)
        {
            if (fullPath == null || fullPath.Length == 0)
                continue;

            // Instanciá al enemigo en la entrada del último tile
            GameObject enemyGO = EnemyPool.Instance.GetEnemy("Slow");
            enemyGO.transform.position = spawnPos;
            Enemy enemy = enemyGO.GetComponent<Enemy>();
            enemy.enemyType = "Slow";
            enemy.InitializePath(fullPath, core, this, gridManager);

            yield return new WaitForSeconds(1f);
        }

        // BOSS/MINIBOSS (idéntico pero podés cambiar el tipo)
        if (isBossWave)
        {
            GameObject bossGO = EnemyPool.Instance.GetEnemy("Boss");
            bossGO.transform.position = spawnPos;
            Enemy bossEnemy = bossGO.GetComponent<Enemy>();
            bossEnemy.enemyType = "Boss";
            bossEnemy.InitializePath(fullPath, core, this, gridManager);
            yield return new WaitForSeconds(1f);
        }
        else if (isMiniBossWave)
        {
            GameObject miniBossGO = EnemyPool.Instance.GetEnemy("MiniBoss");
            miniBossGO.transform.position = spawnPos;
            Enemy miniBoss = miniBossGO.GetComponent<Enemy>();
            miniBoss.enemyType = "MiniBoss";
            miniBoss.InitializePath(fullPath, core, this, gridManager);
            yield return new WaitForSeconds(1f);
        }
    }


    void SpawnEnemy()
    {
        Vector3[] pathPositions = gridManager.GetFullPathPositionsForward();
        if (pathPositions == null || pathPositions.Length == 0)
        {
            Debug.LogError("[WaveSpawner] Path inválido, no se puede spawnear enemigo.");
            return;
        }

        GameObject enemyGO = EnemyPool.Instance.GetEnemy("Slow"); // o el tipo que quieras
        if (enemyGO == null)
        {
            Debug.LogError("[WaveSpawner] EnemyPool no devolvió un prefab válido.");
            return;
        }

        // ¡Spawnear SIEMPRE en el ÚLTIMO punto del path!
        enemyGO.transform.position = gridManager.GetLastTileEntryWorldPos();

        Enemy enemy = enemyGO.GetComponent<Enemy>();
        enemy.enemyType = "Slow";
        enemy.InitializePath(pathPositions, core, this, gridManager);
    }


    void SpawnSpecificEnemy(Vector3[] pathPositions, string enemyType)
    {
        if (pathPositions == null || pathPositions.Length == 0)
        {
            Debug.LogError("[WaveSpawner] Path inválido, no se puede spawnear enemigo.");
            return;
        }

        GameObject enemyGO = EnemyPool.Instance.GetEnemy(enemyType);
        if (enemyGO == null)
        {
            Debug.LogError("[WaveSpawner] EnemyPool no devolvió un prefab válido para " + enemyType);
            return;
        }

        // SPAWNEA AL FINAL DEL ARRAY
        enemyGO.transform.position = gridManager.GetLastTileEntryWorldPos();

        // PASA EL ARRAY Y UN FLAG DE DIRECCIÓN
        Enemy enemy = enemyGO.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("[WaveSpawner] El prefab no tiene script Enemy");
            return;
        }
        enemy.enemyType = enemyType;
        // PASA UN FLAG PARA IR AL REVÉS, o reordena el array
        enemy.InitializePath(pathPositions, core, this, gridManager);
    }




    public void RegisterGoldTurret(GoldTurret turret)
    {
        if (!goldTurrets.Contains(turret))
            goldTurrets.Add(turret);
    }
}