using UnityEngine;
using System.Collections.Generic;

public class AutoPathBuilder : MonoBehaviour
{
    [Header("Path Prefabs")]
    public GameObject downLeftPrefab;
    public GameObject downRightPrefab;
    public GameObject downStraightPrefab;
    public GameObject leftLeftTurnPrefab;
    public GameObject leftRightTurnPrefab;
    public GameObject leftStraightPrefab;
    public GameObject rightLeftTurnPrefab;
    public GameObject rightRightTurnPrefab;
    public GameObject rightStraightPrefab;
    public GameObject upLeftTurnPrefab;
    public GameObject upRightTurnPrefab;
    public GameObject upStraightPrefab;

    [Header("Generation Settings")]
    public int extensionLength = 3; // Cuántas piezas agregar por hotkey
    public bool enableAutoBuilding = true;

    [Header("Integration")]
    public bool integrateWithGridManager = true;

    private GridManager gridManager;
    private WaveSpawner waveSpawner;
    private Dictionary<KeyCode, (GameObject prefab, Vector3 direction, string name)> hotkeyToPathData;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        waveSpawner = FindObjectOfType<WaveSpawner>();
        
        SetupHotkeys();
        ShowInstructions();
    }

    void SetupHotkeys()
    {
        hotkeyToPathData = new Dictionary<KeyCode, (GameObject, Vector3, string)>
        {
            // Cambiar a Vector2Int sería mejor, pero si quieres mantener Vector3:
            { KeyCode.Alpha1, (upStraightPrefab, new Vector3(0, 0, 1), "ARRIBA") },     // Vector3.forward
            { KeyCode.Alpha2, (downStraightPrefab, new Vector3(0, 0, -1), "ABAJO") },   // Vector3.back
            { KeyCode.Alpha3, (leftStraightPrefab, new Vector3(-1, 0, 0), "IZQUIERDA") }, // Vector3.left
            { KeyCode.Alpha4, (rightStraightPrefab, new Vector3(1, 0, 0), "DERECHA") },   // Vector3.right
        
            // Resto de las curvas...
            { KeyCode.Alpha5, (upLeftTurnPrefab, new Vector3(0, 0, 1), "CURVA ARR-IZQ") },
            { KeyCode.Alpha6, (upRightTurnPrefab, new Vector3(0, 0, 1), "CURVA ARR-DER") },
            { KeyCode.Alpha7, (downLeftPrefab, new Vector3(0, 0, -1), "CURVA ABA-IZQ") },
            { KeyCode.Alpha8, (downRightPrefab, new Vector3(0, 0, -1), "CURVA ABA-DER") },
        
            { KeyCode.Alpha9, (leftLeftTurnPrefab, new Vector3(-1, 0, 0), "L IZQUIERDA") },
            { KeyCode.Alpha0, (rightRightTurnPrefab, new Vector3(1, 0, 0), "L DERECHA") }
        };
    }

    void Update()
    {
        if (!enableAutoBuilding) return;

        HandleHotkeyInput();
    }

    // 1. Corregir la llamada al método en HandleHotkeyInput()
    void HandleHotkeyInput()
    {
        // Generar extensiones de camino como F1
        foreach (var kvp in hotkeyToPathData)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                // CORREGIDO: Quitar () extra y convertir Vector3 a Vector2Int
                Vector2Int direction2D = new Vector2Int((int)kvp.Value.direction.x, (int)kvp.Value.direction.z);
                ExtendMapInDirection(direction2D, kvp.Value.name);
            }
        }

        // Controles adicionales
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearExtensions();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleAutoBuilding();
        }

        // Crear bifurcación circular como F1
        if (Input.GetKeyDown(KeyCode.T))
        {
            ForceCircularTile();
        }

        // Generar múltiples extensiones como F4
        if (Input.GetKeyDown(KeyCode.G))
        {
            GenerateMultipleExtensions();
        }
    }

    void ExtendMapInDirection(Vector2Int direction, string directionName)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager no encontrado");
            return;
        }

        Debug.Log($"🔨 Extendiendo mapa hacia {directionName} desde currentPathEnd: {gridManager.currentPathEnd}");

        // Crear una expansión de tile falsa similar a como hace F1
        int fakeWaveNumber = UnityEngine.Random.Range(1, 10);
        
        // Forzar dirección específica modificando temporalmente el sistema
        ForceDirectionalExpansion(direction, directionName, fakeWaveNumber);
    }

    void ForceDirectionalExpansion(Vector2Int direction, string directionName, int waveNumber)
    {
        // Simular el comportamiento de ApplyRandomValidTileExpansion pero en dirección específica
        var customTile = CreateDirectionalTile(direction, directionName);
        
        if (customTile != null)
        {
            Vector3 worldPosition = new Vector3(gridManager.currentPathEnd.x * gridManager.cellSize, 0, gridManager.currentPathEnd.y * gridManager.cellSize);
            
            // Aplicar la expansión usando el método del GridManager
            ApplyCustomTileExpansion(customTile, worldPosition, direction);
            
            Debug.Log($"✅ Extensión aplicada hacia {directionName}");
            
            // Actualizar enemigos si hay alguno activo (como F1)
            if (waveSpawner != null)
            {
                waveSpawner.SendMessage("UpdateEnemyPaths", SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    GridManager.TileExpansion CreateDirectionalTile(Vector2Int direction, string directionName)
    {
        // Crear un tile personalizado en la dirección especificada
        Vector2Int[] offsets;
        
        if (direction == Vector2Int.up)
        {
            offsets = new Vector2Int[]
            {
                new Vector2Int(0, 0),
                Vector2Int.up,
                Vector2Int.up * 2,
                Vector2Int.up * 3
            };
        }
        else if (direction == Vector2Int.right)
        {
            offsets = new Vector2Int[]
            {
                new Vector2Int(0, 0),
                Vector2Int.right,
                Vector2Int.right * 2,
                Vector2Int.right * 3
            };
        }
        else if (direction == Vector2Int.left)
        {
            offsets = new Vector2Int[]
            {
                new Vector2Int(0, 0),
                Vector2Int.left,
                Vector2Int.left * 2,
                Vector2Int.left * 3
            };
        }
        else if (direction == Vector2Int.down)
        {
            offsets = new Vector2Int[]
            {
                new Vector2Int(0, 0),
                Vector2Int.down,
                Vector2Int.down * 2,
                Vector2Int.down * 3
            };
        }
        else
        {
            return null;
        }

        return new GridManager.TileExpansion($"Custom-{directionName}", offsets);
    }

    void ApplyCustomTileExpansion(GridManager.TileExpansion tile, Vector3 worldPosition, Vector2Int direction)
    {
        // Usar reflexión o acceso directo al método del GridManager
        // Como alternativa, duplicar la lógica de ApplyTileExpansionAtWorldPosition

        gridManager.tileCount++;
        GameObject tileParent = new GameObject($"CustomTile-{gridManager.tileCount}");
        tileParent.transform.parent = gridManager.transform;

        Vector2Int pathDirection = direction;
        List<Vector2Int> rotatedOffsets = gridManager.RotateOffsets(tile.pathOffsets, pathDirection);

        Vector2Int tileOffset = Vector2Int.zero;
        if (pathDirection == Vector2Int.up) tileOffset = new Vector2Int(-tile.tileSize.x / 2, 1);
        else if (pathDirection == Vector2Int.right) tileOffset = new Vector2Int(1, -tile.tileSize.y / 2);
        else if (pathDirection == Vector2Int.left) tileOffset = new Vector2Int(-tile.tileSize.x, -tile.tileSize.y / 2);

        Vector2Int bottomLeft = gridManager.currentPathEnd + tileOffset;
        Vector2Int startPath = gridManager.currentPathEnd + pathDirection;

        // Crear celdas del tile 5x5
        for (int x = 0; x < tile.tileSize.x; x++)
        {
            for (int y = 0; y < tile.tileSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(bottomLeft.x + x, bottomLeft.y + y);
                Vector3 spawnPos = new Vector3(pos.x * gridManager.cellSize, 0, pos.y * gridManager.cellSize);

                if (!gridManager.gridCells.ContainsKey(pos))
                {
                    gridManager.gridCells[pos] = Instantiate(gridManager.cellPrefab, spawnPos, Quaternion.identity, tileParent.transform);
                }
                gridManager.CrearCelda(pos);
            }
        }

        // Crear el camino principal
        for (int i = 0; i < rotatedOffsets.Count; i++)
        {
            Vector2Int pathPos = startPath + rotatedOffsets[i];
            Vector3 spawnPos = new Vector3(pathPos.x * gridManager.cellSize, 0, pathPos.y * gridManager.cellSize);

            if (gridManager.gridCells.ContainsKey(pathPos))
            {
                Destroy(gridManager.gridCells[pathPos]);
                gridManager.gridCells.Remove(pathPos);
            }
            gridManager.gridCells[pathPos] = Instantiate(gridManager.pathPrefab, spawnPos, Quaternion.identity, tileParent.transform);
            gridManager.pathPositions.Add(spawnPos);

            gridManager.CrearCelda(pathPos);
            if (i > 0)
            {
                gridManager.ConectarCeldas(startPath + rotatedOffsets[i - 1], pathPos, 1);
            }
            else
            {
                gridManager.ConectarCeldas(gridManager.currentPathEnd, pathPos, 1);
            }
        }

        // Actualizar currentPathEnd
        gridManager.currentPathEnd = startPath + rotatedOffsets[^1];

        gridManager.placedTiles.Add(new GridManager.PlacedTileData(tile.tileName, bottomLeft));
        
        Debug.Log($"🎯 Nuevo currentPathEnd: {gridManager.currentPathEnd}");
    }

    void ForceCircularTile()
    {
        if (gridManager == null) return;

        Debug.Log("🔄 Forzando creación de tile circular (como F1 con bifurcaciones)");
        
        // Forzar una redoma circular
        int fakeWaveNumber = 2; // Número par para forzar circular
        gridManager.ApplyRandomValidTileExpansion(fakeWaveNumber);
        
        Debug.Log("✅ Tile circular forzado");
    }

    void GenerateMultipleExtensions()
    {
        if (gridManager == null) return;

        Debug.Log("🚀 Generando múltiples extensiones rápidas...");
        StartCoroutine(MultipleExtensionsCoroutine());
    }

    System.Collections.IEnumerator MultipleExtensionsCoroutine()
    {
        // Simular el comportamiento de F4
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        string[] dirNames = { "ARRIBA", "DERECHA", "ARRIBA", "IZQUIERDA" };

        for (int i = 0; i < directions.Length; i++)
        {
            ExtendMapInDirection(directions[i], dirNames[i]);
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("✅ Múltiples extensiones completadas");
    }

    void ClearExtensions()
    {
        // Limpiar tiles personalizados
        GameObject[] customTiles = GameObject.FindGameObjectsWithTag("CustomTile");
        foreach (GameObject tile in customTiles)
        {
            DestroyImmediate(tile);
        }

        // También buscar por nombre
        for (int i = 0; i < gridManager.transform.childCount; i++)
        {
            Transform child = gridManager.transform.GetChild(i);
            if (child.name.Contains("CustomTile"))
            {
                DestroyImmediate(child.gameObject);
                i--; // Ajustar índice después de destruir
            }
        }

        Debug.Log("🗑️ Extensiones personalizadas limpiadas");
    }

    void ToggleAutoBuilding()
    {
        enableAutoBuilding = !enableAutoBuilding;
        Debug.Log($"Auto-construcción: {(enableAutoBuilding ? "✅ ACTIVADA" : "❌ DESACTIVADA")}");
    }

    void ShowInstructions()
    {
        Debug.Log("🛤️ === AUTO PATH BUILDER (Como F1) ===");
        Debug.Log("EXTENDER MAPA DESDE PUNTO ACTUAL:");
        Debug.Log("1 = Extender ARRIBA | 2 = Extender ABAJO | 3 = Extender IZQUIERDA | 4 = Extender DERECHA");
        Debug.Log("5 = Curva ARR-IZQ | 6 = Curva ARR-DER | 7 = Curva ABA-IZQ | 8 = Curva ABA-DER");
        Debug.Log("9 = L IZQUIERDA | 0 = L DERECHA");
        Debug.Log("");
        Debug.Log("ESPECIALES:");
        Debug.Log("T = Forzar Tile Circular (como F1 con bifurcaciones)");
        Debug.Log("G = Múltiples extensiones rápidas (como F4)");
        Debug.Log("C = Limpiar extensiones personalizadas | B = Toggle sistema");
        Debug.Log("");
        Debug.Log("💡 Funciona igual que F1: extiende desde currentPathEnd del GridManager");
        Debug.Log("============================================");
    }

    // Método para debugging
    [ContextMenu("Mostrar Estado GridManager")]
    public void ShowGridManagerState()
    {
        if (gridManager != null)
        {
            Debug.Log("📊 === ESTADO GRIDMANAGER ===");
            Debug.Log($"CurrentPathEnd: {gridManager.currentPathEnd}");
            Debug.Log($"Tiles colocados: {gridManager.placedTiles.Count}");
            Debug.Log($"Posiciones de camino: {gridManager.pathPositions.Count}");
            
            var endpoints = gridManager.ObtenerPuntosFinales();
            Debug.Log($"Puntos finales: {endpoints.Count}");
            foreach (var endpoint in endpoints)
            {
                Debug.Log($"  - {endpoint}");
            }
        }
        else
        {
            Debug.LogError("GridManager no encontrado");
        }
    }
}