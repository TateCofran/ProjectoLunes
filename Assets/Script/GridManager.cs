using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Script; 

public class GridManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject cellPrefab;
    public GameObject pathPrefab;
    public GameObject corePrefab;
    public int width = 5;
    public int height = 5;

    public float cellSize = 1.0f;

    // Grafo
    GrafoMA grafo = new GrafoMA();
    Dictionary<Vector2Int, int> celdaToVertice = new Dictionary<Vector2Int, int>(); 
    int proximoVertice = 0;

    // Mapa
    public Dictionary<Vector2Int, GameObject> gridCells = new Dictionary<Vector2Int, GameObject>();
    public List<Vector3> pathPositions = new List<Vector3>();
    public List<PlacedTileData> placedTiles = new List<PlacedTileData>();

    public Vector2Int currentPathEnd;
    private int tileCount = 0;

    private List<Vector2Int> caminoOptimoDebug = null;

    public class TileExpansion
    {
        public string tileName;
        public Vector2Int[] pathOffsets;
        public Vector2Int tileSize;

        public TileExpansion(string name, Vector2Int[] pathOffsets)
        {
            tileName = name;
            this.pathOffsets = pathOffsets;
            tileSize = new Vector2Int(5, 5);
        }
    }

    public class PlacedTileData
    {
        public string tileName;
        public Vector2Int basePosition;

        public PlacedTileData(string name, Vector2Int pos)
        {
            tileName = name;
            basePosition = pos;
        }
    }

    void Start()
    {
        grafo.InicializarGrafo();
        GenerateInitialGrid();
    }

    void GenerateInitialGrid()
    {
        placedTiles.Clear();
        gridCells.Clear();
        pathPositions.Clear();
        celdaToVertice.Clear();
        proximoVertice = 0;
        grafo.InicializarGrafo();

        GameObject tileCore = new GameObject("Tile-Core");
        tileCore.transform.parent = this.transform;

        // Grilla base (tiles de celda)
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                Vector3 worldPos = new Vector3(x * cellSize, 0, z * cellSize);
                gridCells[pos] = Instantiate(cellPrefab, worldPos, Quaternion.identity, tileCore.transform);
                CrearCelda(pos); 
            }
        }

        // Posición del core: centro en X, parte inferior en Z
        Vector2Int corePos = new Vector2Int(width / 2, 0);
        Vector3 coreWorldPos = new Vector3(corePos.x * cellSize, 0, corePos.y * cellSize);

        // Eliminar celda del core (si está)
        if (gridCells.ContainsKey(corePos))
        {
            Destroy(gridCells[corePos]);
            gridCells.Remove(corePos);
        }

        // Instanciar core y sumarlo como vértice del grafo
        Instantiate(corePrefab, coreWorldPos, Quaternion.identity, tileCore.transform);
        CrearCelda(corePos); 

        pathPositions.Add(coreWorldPos);
        currentPathEnd = corePos;

        // Guardar la posición de tile inicial (tile de 5x5)
        Vector2Int initialBottomLeft = new Vector2Int(corePos.x - 2, 0);
        placedTiles.Add(new PlacedTileData("Inicial", initialBottomLeft));

        // Generar camino hacia arriba hasta el borde e integrarlo al grafo
        for (int z = 1; z < height; z++)
        {
            Vector2Int pathPos = new Vector2Int(corePos.x, z);
            Vector3 pathWorldPos = new Vector3(pathPos.x * cellSize, 0, pathPos.y * cellSize);

            if (gridCells.ContainsKey(pathPos))
            {
                Destroy(gridCells[pathPos]);
                gridCells.Remove(pathPos);
            }

            GameObject pathGO = Instantiate(pathPrefab, pathWorldPos, Quaternion.identity, tileCore.transform);

            pathPositions.Add(pathWorldPos);

            // Agregar vértice y conectar arista en el grafo
            CrearCelda(pathPos);
            ConectarCeldas(currentPathEnd, pathPos, 1); 
            currentPathEnd = pathPos;
        }
    }

    public void CrearCelda(Vector2Int pos)
    {
        if (!celdaToVertice.ContainsKey(pos))
        {
            grafo.AgregarVertice(proximoVertice);
            celdaToVertice[pos] = proximoVertice;
            proximoVertice++;
        }
    }

    public void ConectarCeldas(Vector2Int pos1, Vector2Int pos2, int peso = 1)
    {
        if (celdaToVertice.ContainsKey(pos1) && celdaToVertice.ContainsKey(pos2))
        {
            int v1 = celdaToVertice[pos1];
            int v2 = celdaToVertice[pos2];
            grafo.AgregarArista(0, v1, v2, peso);
            grafo.AgregarArista(0, v2, v1, peso);
        }
    }

    public bool SePuedeExpandir(Vector2Int pos)
    {
        return !celdaToVertice.ContainsKey(pos);
    }
   public void ApplyTileExpansionAtWorldPosition(TileExpansion tile, Vector3 worldPosition)
{
    tileCount++;
    GameObject tileParent = new GameObject($"Tile-{tileCount}");
    tileParent.transform.parent = this.transform;

    Vector2Int pathDirection = GetPathDirection();
    List<Vector2Int> rotatedOffsets = RotateOffsets(tile.pathOffsets, pathDirection);

    Vector2Int tileOffset = Vector2Int.zero;
    if (pathDirection == Vector2Int.up) tileOffset = new Vector2Int(-tile.tileSize.x / 2, 1);
    else if (pathDirection == Vector2Int.right) tileOffset = new Vector2Int(1, -tile.tileSize.y / 2);
    else if (pathDirection == Vector2Int.down) tileOffset = new Vector2Int(-tile.tileSize.x / 2, -tile.tileSize.y);
    else if (pathDirection == Vector2Int.left) tileOffset = new Vector2Int(-tile.tileSize.x, -tile.tileSize.y / 2);

    Vector2Int bottomLeft = currentPathEnd + tileOffset;
    Vector2Int startPath = currentPathEnd + pathDirection;

    // Crear celdas del tile 5x5
    for (int x = 0; x < tile.tileSize.x; x++)
    {
        for (int y = 0; y < tile.tileSize.y; y++)
        {
            Vector2Int pos = new Vector2Int(bottomLeft.x + x, bottomLeft.y + y);
            Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);

            if (!gridCells.ContainsKey(pos))
            {
                gridCells[pos] = Instantiate(cellPrefab, spawnPos, Quaternion.identity, tileParent.transform);
            }
            CrearCelda(pos);
        }
    }

    // Crear el camino principal
    Vector2Int prevPath = startPath;
    for (int i = 0; i < rotatedOffsets.Count; i++)
    {
        Vector2Int pathPos = startPath + rotatedOffsets[i];
        Vector3 spawnPos = new Vector3(pathPos.x * cellSize, 0, pathPos.y * cellSize);

        if (gridCells.ContainsKey(pathPos))
        {
            Destroy(gridCells[pathPos]);
            gridCells.Remove(pathPos);
        }
        gridCells[pathPos] = Instantiate(pathPrefab, spawnPos, Quaternion.identity, tileParent.transform);
        pathPositions.Add(spawnPos);

        CrearCelda(pathPos);
        if (i > 0)
        {
            ConectarCeldas(startPath + rotatedOffsets[i - 1], pathPos, 1);
        }
        else
        {
            ConectarCeldas(currentPathEnd, pathPos, 1);
        }
        prevPath = pathPos;
    }

    // SI ES UNA BIFURCACIÓN EN T
    if (tile.tileName == "T-Bifurcacion")
    {
        Debug.Log("[GridManager] Creando bifurcación en T");
        CrearBifurcacionT(startPath, rotatedOffsets, pathDirection, tileParent);
        // No actualizar currentPathEnd aquí
    }
    else
    {
        // Solo actualizar currentPathEnd si NO es bifurcación
        currentPathEnd = startPath + rotatedOffsets[^1];
    }

    placedTiles.Add(new PlacedTileData(tile.tileName, bottomLeft));
}
   private void CrearBifurcacionT(Vector2Int startPath, List<Vector2Int> mainOffsets, Vector2Int mainDirection, GameObject tileParent)
{
    // El punto de bifurcación está al final del camino principal corto
    Vector2Int bifurcationPoint = startPath + mainOffsets[mainOffsets.Count - 1];
    
    Debug.Log($"[GridManager] Punto de bifurcación: {bifurcationPoint}");

    // Continuar el camino central
    Vector2Int centralEnd = bifurcationPoint;
    for (int i = 1; i <= 2; i++)
    {
        Vector2Int pos = bifurcationPoint + mainDirection * i;
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) break;
        
        Vector3 worldPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
        if (gridCells.ContainsKey(pos))
        {
            Destroy(gridCells[pos]);
            gridCells.Remove(pos);
        }
        gridCells[pos] = Instantiate(pathPrefab, worldPos, Quaternion.identity, tileParent.transform);
        pathPositions.Add(worldPos);
        
        CrearCelda(pos);
        ConectarCeldas(centralEnd, pos, 1);
        centralEnd = pos;
    }

    // Determinar direcciones laterales
    Vector2Int leftDir, rightDir;
    if (mainDirection == Vector2Int.up || mainDirection == Vector2Int.down)
    {
        leftDir = Vector2Int.left;
        rightDir = Vector2Int.right;
    }
    else
    {
        leftDir = Vector2Int.down;
        rightDir = Vector2Int.up;
    }

    // Crear rama izquierda
    Vector2Int leftEnd = bifurcationPoint;
    for (int i = 1; i <= 2; i++)
    {
        Vector2Int pos = bifurcationPoint + leftDir * i;
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) break;
        
        Vector3 worldPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
        if (gridCells.ContainsKey(pos))
        {
            Destroy(gridCells[pos]);
            gridCells.Remove(pos);
        }
        gridCells[pos] = Instantiate(pathPrefab, worldPos, Quaternion.identity, tileParent.transform);
        pathPositions.Add(worldPos);
        
        CrearCelda(pos);
        ConectarCeldas(leftEnd, pos, 1);
        leftEnd = pos;
    }

    // Crear rama derecha
    Vector2Int rightEnd = bifurcationPoint;
    for (int i = 1; i <= 2; i++)
    {
        Vector2Int pos = bifurcationPoint + rightDir * i;
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) break;
        
        Vector3 worldPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
        if (gridCells.ContainsKey(pos))
        {
            Destroy(gridCells[pos]);
            gridCells.Remove(pos);
        }
        gridCells[pos] = Instantiate(pathPrefab, worldPos, Quaternion.identity, tileParent.transform);
        pathPositions.Add(worldPos);
        
        CrearCelda(pos);
        ConectarCeldas(rightEnd, pos, 1);
        rightEnd = pos;
    }

    // IMPORTANTE: Actualizar currentPathEnd al punto central final
    currentPathEnd = centralEnd;
    
    Debug.Log($"[GridManager] Bifurcación creada. Rama izq: {leftEnd}, Centro: {centralEnd}, Rama der: {rightEnd}");
}
  private void CrearRamasBifurcacion(Vector2Int startPath, List<Vector2Int> mainOffsets, Vector2Int mainDirection, GameObject tileParent)
{
    // Punto de bifurcación: mitad del camino principal
    Vector2Int bifurcationPoint = startPath + mainOffsets[2];
    
    Debug.Log($"[GridManager] Creando bifurcación en punto: {bifurcationPoint}");
    
    // Determinar las direcciones perpendiculares
    Vector2Int[] sideDirections = new Vector2Int[2];
    if (mainDirection == Vector2Int.up || mainDirection == Vector2Int.down)
    {
        sideDirections[0] = Vector2Int.left;
        sideDirections[1] = Vector2Int.right;
    }
    else
    {
        sideDirections[0] = Vector2Int.up;
        sideDirections[1] = Vector2Int.down;
    }
    
    Debug.Log($"[GridManager] Direcciones de ramas: {sideDirections[0]}, {sideDirections[1]}");

    // Guardar los puntos finales de cada rama
    List<Vector2Int> branchEndpoints = new List<Vector2Int>();

    // Crear rama izquierda/arriba
    Vector2Int lastLeftPos = bifurcationPoint;
    for (int i = 1; i <= 2; i++)
    {
        Vector2Int branchPos = bifurcationPoint + sideDirections[0] * i;
        
        if (branchPos.x < 0 || branchPos.x >= width || branchPos.y < 0 || branchPos.y >= height)
        {
            Debug.LogWarning($"[GridManager] Rama izquierda fuera de límites en {branchPos}");
            break;
        }
            
        Vector3 spawnPos = new Vector3(branchPos.x * cellSize, 0, branchPos.y * cellSize);
        
        if (gridCells.ContainsKey(branchPos))
        {
            Destroy(gridCells[branchPos]);
            gridCells.Remove(branchPos);
        }
        
        gridCells[branchPos] = Instantiate(pathPrefab, spawnPos, Quaternion.identity, tileParent.transform);
        pathPositions.Add(spawnPos);
        
        CrearCelda(branchPos);
        
        if (i == 1)
            ConectarCeldas(bifurcationPoint, branchPos, 1);
        else
            ConectarCeldas(lastLeftPos, branchPos, 1);
            
        lastLeftPos = branchPos;
    }
    if (lastLeftPos != bifurcationPoint)
        branchEndpoints.Add(lastLeftPos);

    // Crear rama derecha/abajo
    Vector2Int lastRightPos = bifurcationPoint;
    for (int i = 1; i <= 2; i++)
    {
        Vector2Int branchPos = bifurcationPoint + sideDirections[1] * i;
        
        if (branchPos.x < 0 || branchPos.x >= width || branchPos.y < 0 || branchPos.y >= height)
        {
            Debug.LogWarning($"[GridManager] Rama derecha fuera de límites en {branchPos}");
            break;
        }
            
        Vector3 spawnPos = new Vector3(branchPos.x * cellSize, 0, branchPos.y * cellSize);
        
        if (gridCells.ContainsKey(branchPos))
        {
            Destroy(gridCells[branchPos]);
            gridCells.Remove(branchPos);
        }
        
        gridCells[branchPos] = Instantiate(pathPrefab, spawnPos, Quaternion.identity, tileParent.transform);
        pathPositions.Add(spawnPos);
        
        CrearCelda(branchPos);
        
        if (i == 1)
            ConectarCeldas(bifurcationPoint, branchPos, 1);
        else
            ConectarCeldas(lastRightPos, branchPos, 1);
            
        lastRightPos = branchPos;
    }
    if (lastRightPos != bifurcationPoint)
        branchEndpoints.Add(lastRightPos);
    
    Debug.Log($"[GridManager] Bifurcación creada con {branchEndpoints.Count} puntos finales");
}
public List<Vector2Int> ObtenerPuntosFinales()
{
    List<Vector2Int> puntosFinales = new List<Vector2Int>();
    
    foreach (var kvp in celdaToVertice)
    {
        Vector2Int pos = kvp.Key;
        int vertice = kvp.Value;
        
        // Contar conexiones
        int conexiones = 0;
        for (int i = 0; i < grafo.cantNodos; i++)
        {
            if (i != vertice && grafo.ExisteArista(vertice, i))
                conexiones++;
        }
        
        // Si solo tiene 1 conexión y no es el core, es un punto final
        Vector2Int corePos = new Vector2Int(width / 2, 0);
        if (conexiones == 1 && pos != corePos)
        {
            puntosFinales.Add(pos);
        }
    }
    
    Debug.Log($"[GridManager] Puntos finales encontrados: {puntosFinales.Count} - {string.Join(", ", puntosFinales)}");
    return puntosFinales;
}

    public Vector2Int GetPathDirection()
    {
        if (pathPositions.Count < 2)
            return Vector2Int.up;
        Vector3 penultimo = pathPositions[^2];
        Vector3 ultimo = pathPositions[^1];

        Vector2Int from = new Vector2Int(Mathf.RoundToInt(penultimo.x / cellSize), Mathf.RoundToInt(penultimo.z / cellSize));
        Vector2Int to = new Vector2Int(Mathf.RoundToInt(ultimo.x / cellSize), Mathf.RoundToInt(ultimo.z / cellSize));

        return to - from;
    }

    public Vector2Int GetLastPathGridPosition()
    {
        if (pathPositions.Count == 0)
            return Vector2Int.zero;
        Vector3 lastWorld = pathPositions[^1];
        return new Vector2Int(Mathf.RoundToInt(lastWorld.x / cellSize), Mathf.RoundToInt(lastWorld.z / cellSize));
    }

    // Rotar offsets
    public List<Vector2Int> RotateOffsets(Vector2Int[] offsets, Vector2Int dir)
    {
        if (dir == Vector2Int.up) return offsets.ToList();
        if (dir == Vector2Int.right) return offsets.Select(o => new Vector2Int(o.y, -o.x)).ToList();
        if (dir == Vector2Int.down) return offsets.Select(o => new Vector2Int(-o.x, -o.y)).ToList();
        if (dir == Vector2Int.left) return offsets.Select(o => new Vector2Int(-o.y, o.x)).ToList();
        return offsets.ToList();
    }
    public List<TileExpansion> GetTileOptions()
    {
        List<TileExpansion> tiles = new()
        {
            new TileExpansion("Recto", new[]
            {
                new Vector2Int(0, 0),
                Vector2Int.up,
                Vector2Int.up * 2,
                Vector2Int.up * 3,
                Vector2Int.up * 4
            }),

            new TileExpansion("L-Shape", new[]
            {
                new Vector2Int(0, 0),
                Vector2Int.up,
                Vector2Int.up * 2,
                Vector2Int.up * 2 + Vector2Int.right,
                Vector2Int.up * 2 + Vector2Int.right * 2
            }),

            new TileExpansion("L-Inverso", new[]
            {
                new Vector2Int(0, 0),
                Vector2Int.up,
                Vector2Int.up * 2,
                Vector2Int.up * 2 + Vector2Int.left,
                Vector2Int.up * 2 + Vector2Int.left * 2
            }),
        
            // NUEVO: Tile T-Bifurcacion
            new TileExpansion("T-Bifurcacion", new[] {
                new Vector2Int( 0, 0),   // base
                new Vector2Int( 0, 1),   // tronco
                new Vector2Int( 0, 2),
                new Vector2Int( 0, 3),
                // ramas izquierda de 3 celdas
                new Vector2Int(-1, 3),
                new Vector2Int(-2, 3),
                new Vector2Int(-3, 3),
                // ramas derecha de 3 celdas
                new Vector2Int( 1, 3),
                new Vector2Int( 2, 3),
                new Vector2Int( 3, 3)
            })
        };
        return tiles;
    }

    public void ApplyRandomValidTileExpansion(int waveNumber = 0)
    {
        List<TileExpansion> tileOptions = GetTileOptions();
        List<string> disabledTileNames = GetDisabledTileNamesFromNextAdyacents();

        List<TileExpansion> validTiles = tileOptions
            .Where(t => !disabledTileNames.Contains(t.tileName))
            .ToList();

        if (validTiles == null || validTiles.Count == 0)
        {
            Debug.LogWarning("No hay tiles válidos para expandir en esta dirección.");
            return;
        }

        TileExpansion selectedTile;
    
        // Forzar bifurcación cada 3 oleadas (oleadas 3, 6, 9, etc)
        bool shouldForceBifurcation = (waveNumber > 0 && waveNumber % 3 == 0);
    
        if (shouldForceBifurcation && validTiles.Any(t => t.tileName == "T-Bifurcacion"))
        {
            selectedTile = validTiles.First(t => t.tileName == "T-Bifurcacion");
            Debug.Log($"[GridManager] FORZANDO BIFURCACIÓN en oleada {waveNumber}");
        }
        else
        {
            // Selección aleatoria normal
            int randomIndex = UnityEngine.Random.Range(0, validTiles.Count);
            selectedTile = validTiles[randomIndex];
        }
    
        Debug.Log($"[GridManager] Tile seleccionado: {selectedTile.tileName}");

        Vector3 worldPosition = new Vector3(currentPathEnd.x * cellSize, 0, currentPathEnd.y * cellSize);
        ApplyTileExpansionAtWorldPosition(selectedTile, worldPosition);
    }


    public List<string> GetDisabledTileNamesFromNextAdyacents()
    {
        List<string> disabledTiles = new();

        Vector2Int pathDir = GetPathDirection();
        Vector2Int tileOffset = Vector2Int.zero;

        if (pathDir == Vector2Int.up)
            tileOffset = new Vector2Int(-5 / 2, 1);
        else if (pathDir == Vector2Int.right)
            tileOffset = new Vector2Int(1, -5 / 2);
        else if (pathDir == Vector2Int.down)
            tileOffset = new Vector2Int(-5 / 2, -5);
        else if (pathDir == Vector2Int.left)
            tileOffset = new Vector2Int(-5, -5 / 2);
        disabledTiles.Remove("T-Bifurcacion");
        Vector2Int nextBottomLeft = GetLastPathGridPosition() + tileOffset;

        Vector2Int[] localDirs = new[]
        {
            Vector2Int.up * 5,
            Vector2Int.down * 5,
            Vector2Int.left * 5,
            Vector2Int.right * 5
        };

        HashSet<Vector2Int> allTilePositions = new(placedTiles.Select(p => p.basePosition));

        foreach (var dir in localDirs)
        {
            Vector2Int adjacent = nextBottomLeft + dir;
            if (adjacent == currentPathEnd) continue;

            if (!allTilePositions.Contains(adjacent)) continue;

            Vector2Int delta = adjacent - nextBottomLeft;
            string sugerido = "";

            if (delta == pathDir * 5)
                sugerido = "Recto";
            else if (
                (pathDir == Vector2Int.up && delta == Vector2Int.right * 5) ||
                (pathDir == Vector2Int.right && delta == Vector2Int.down * 5) ||
                (pathDir == Vector2Int.down && delta == Vector2Int.left * 5) ||
                (pathDir == Vector2Int.left && delta == Vector2Int.up * 5))
                sugerido = "L-Shape";
            else if (
                (pathDir == Vector2Int.up && delta == Vector2Int.left * 5) ||
                (pathDir == Vector2Int.left && delta == Vector2Int.down * 5) ||
                (pathDir == Vector2Int.down && delta == Vector2Int.right * 5) ||
                (pathDir == Vector2Int.right && delta == Vector2Int.up * 5))
                sugerido = "L-Inverso";

            if (!string.IsNullOrEmpty(sugerido) && !disabledTiles.Contains(sugerido))
                disabledTiles.Add(sugerido);
        }
        disabledTiles.Remove("T-Bifurcacion");
        return disabledTiles;
    }
    public Vector3[] GetPathPositions()
    {
        return pathPositions.ToArray();
    }

    public List<Vector2Int> ObtenerCamino(Vector2Int inicio, Vector2Int fin)
    {
        if (celdaToVertice.ContainsKey(inicio) && celdaToVertice.ContainsKey(fin))
        {
            int v1 = celdaToVertice[inicio];
            int v2 = celdaToVertice[fin];
            var caminoVerts = grafo.GetPathBFS(v1, v2, grafo);
            List<Vector2Int> camino = new List<Vector2Int>();
            foreach (int vert in caminoVerts)
            {
                Vector2Int pos = celdaToVertice.FirstOrDefault(x => x.Value == vert).Key;
                camino.Add(pos);
            }
            return camino;
        }
        return null;
    }
    public Vector3[] ObtenerCaminoOptimoWorld(Vector2Int inicio, Vector2Int fin)
    {
        var camino = ObtenerCaminoOptimo(inicio, fin); 
        if (camino == null) return null;
        return camino.Select(pos => new Vector3(pos.x * cellSize, 0, pos.y * cellSize)).ToArray();
    }

    public List<Vector2Int> ObtenerCaminoOptimo(Vector2Int inicio, Vector2Int fin)
    {
        if (!celdaToVertice.ContainsKey(inicio) || !celdaToVertice.ContainsKey(fin))
            return null;

        int vInicio = celdaToVertice[inicio];
        int vFin = celdaToVertice[fin];

        AlgDijkstra.Dijkstra(grafo, vInicio);


        int idxDestino = -1;
        for (int i = 0; i < grafo.cantNodos; i++)
        {
            if (grafo.Etiqs[i] == vFin)
            {
                idxDestino = i;
                break;
            }
        }
        if (idxDestino == -1 || AlgDijkstra.nodos == null || AlgDijkstra.nodos.Length <= idxDestino || AlgDijkstra.nodos[idxDestino] == null)
            return null;

        string pathStr = AlgDijkstra.nodos[idxDestino];
        string[] idsStr = pathStr.Split(',');

        // Convertir de IDs a Vector2Int
        List<Vector2Int> camino = new List<Vector2Int>();
        foreach (var idStr in idsStr)
        {
            if (int.TryParse(idStr, out int id))
            {
                // Buscar la posición correspondiente a ese id
                Vector2Int pos = celdaToVertice.FirstOrDefault(x => x.Value == id).Key;
                if (pos != null) camino.Add(pos);
            }
        }
        return camino;
    }
    public void DebugCaminoOptimo(Vector2Int inicio, Vector2Int fin)
    {
        caminoOptimoDebug = ObtenerCaminoOptimo(inicio, fin);
    }
    void OnDrawGizmos()
    {
        //conexiones del grafo
        if (celdaToVertice != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var from in celdaToVertice.Keys)
            {
                foreach (var to in celdaToVertice.Keys)
                {
                    if (from == to) continue;
                    int vFrom = celdaToVertice[from];
                    int vTo = celdaToVertice[to];
                    if (grafo.ExisteArista(vFrom, vTo))
                    {
                        Vector3 posFrom = new Vector3(from.x * cellSize, 1, from.y * cellSize);
                        Vector3 posTo = new Vector3(to.x * cellSize, 1, to.y * cellSize);
                        Gizmos.DrawLine(posFrom, posTo);
                    }
                }
            }
        }
    }


}
