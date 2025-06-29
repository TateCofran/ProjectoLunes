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
        gridManager.ApplyRandomValidTileExpansion();

        yield return null; // espera a que termine la expansión visual

        // 1. Obtener el camino actualizado
        Vector3[] fullPath = gridManager.GetPathPositions();
        //Debug.Log("[PATH] --- Puntos del camino:");
        for (int i = 0; i < fullPath.Length; i++)
        {
            //Debug.Log($"[{i}] {fullPath[i]}");
        }

        if (fullPath == null || fullPath.Length == 0)
        {
            Debug.LogError("[WaveSpawner] No se generó ningún camino. Abortando oleada.");
            yield break;
        }

        // INVIERTE el camino si el primero es el core
        if (Vector3.Distance(core.transform.position, fullPath[0]) < 0.5f)
        {
            System.Array.Reverse(fullPath);
        }

        Vector3 spawnPos = fullPath[0]; // Ahora el primero es el punto de inicio (lejos del core)

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

        //Debug.Log($"Oleada {currentWave} iniciada. Spawneando {enemiesAlive} enemigos.");

        // Antes de entrar al for:
        Vector3[] path = fullPath.ToArray();
        spawnPos = path[path.Length - 1]; // <- ÚLTIMA POSICIÓN

        Vector3 spawnWorldPos = path[path.Length - 1];
        Vector2Int spawnGridPos = new Vector2Int(
            Mathf.RoundToInt(spawnWorldPos.x / gridManager.cellSize),
            Mathf.RoundToInt(spawnWorldPos.z / gridManager.cellSize)
        );

        Vector3 coreWorldPos = path[0]; // asumiendo que el core está en la primera posición
        Vector2Int coreGridPos = new Vector2Int(
            Mathf.RoundToInt(coreWorldPos.x / gridManager.cellSize),
            Mathf.RoundToInt(coreWorldPos.z / gridManager.cellSize)
        );


        // spawn normales
        for (int i = 0; i < totalEnemies - extraEnemies; i++)
        {
            var go = EnemyPool.Instance.GetEnemy("Slow");
            var e = go.GetComponent<Enemy>();

            go.transform.position = spawnPos; // SPAWN EN EL FINAL

            e.enemyType = "Slow";

            // Pasa el path completo
            e.InitializePath(spawnGridPos, coreGridPos, core, this, gridManager);

            //Debug.Log($"[SPAWN ENEMY] Instanciando enemigo en: {spawnPos}");

            yield return new WaitForSeconds(1f);
        }


        // spawn mini-boss / boss
        if (isMiniBossWave)
            yield return SpawnSpecial("MiniBoss", fullPath, spawnPos);
        else if (isBossWave)
            yield return SpawnSpecial("Boss", fullPath, spawnPos);
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
        // Si ya usás gridManager.corePos, usalo directamente. 
        // Si no, obtenelo de fullPath[0] como antes:
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
        // Otros efectos al matar un enemigo
    }

    public void RegisterGoldTurret(GoldTurret turret)
    {
        if (!goldTurrets.Contains(turret))
            goldTurrets.Add(turret);
    }
}
