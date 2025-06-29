using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.Script; // <- Asegurate que esté bien el namespace

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
    Dictionary<Vector2Int, int> celdaToVertice = new Dictionary<Vector2Int, int>(); // Mapea posición en grilla a id de vértice del grafo
    int proximoVertice = 0;

    // Mapa visual
    public Dictionary<Vector2Int, GameObject> gridCells = new Dictionary<Vector2Int, GameObject>();
    public List<Vector3> pathPositions = new List<Vector3>();
    public List<PlacedTileData> placedTiles = new List<PlacedTileData>();

    // Estado
    public Vector2Int currentPathEnd;
    private int tileCount = 0;

    private List<Vector2Int> caminoOptimoDebug = null;

    // Ejemplo: TileExpansion
    public class TileExpansion
    {
        public string tileName;
        public Vector2Int[] pathOffsets;
        public Vector2Int tileSize;

        public TileExpansion(string name, Vector2Int[] pathOffsets)
        {
            tileName = name;
            this.pathOffsets = pathOffsets;
            tileSize = new Vector2Int(5, 5); // Siempre 5x5 en tu ejemplo
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
                CrearCelda(pos); // <--- AGREGAR al grafo cada celda base
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
        CrearCelda(corePos); // <-- Vértice del núcleo

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
            ConectarCeldas(currentPathEnd, pathPos, 1); // Arista entre camino anterior y actual

            currentPathEnd = pathPos;
        }
    }

    // Crea un vértice/celda en el grafo si no existe
    public void CrearCelda(Vector2Int pos)
    {
        if (!celdaToVertice.ContainsKey(pos))
        {
            grafo.AgregarVertice(proximoVertice);
            celdaToVertice[pos] = proximoVertice;
            proximoVertice++;
        }
    }

    // Crea una arista (bidireccional)
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

    // Valida que no exista la celda ya en el grafo
    public bool SePuedeExpandir(Vector2Int pos)
    {
        return !celdaToVertice.ContainsKey(pos);
    }

    // Aplicar expansión de un tile en el grafo y en la grilla visual
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

        // Crear celdas y vértices del grafo (tiles 5x5)
        for (int x = 0; x < tile.tileSize.x; x++)
        {
            for (int y = 0; y < tile.tileSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(bottomLeft.x + x, bottomLeft.y + y);
                Vector3 spawnPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);

                if (!gridCells.ContainsKey(pos))
                {
                    gridCells[pos] = Instantiate(cellPrefab, spawnPos, Quaternion.identity, tileParent.transform);
                    //gridCells[pos].AddComponent<CellVisual>();
                }
                CrearCelda(pos); // ¡Lo agregás como vértice del grafo!
            }
        }

        // Crear el path visual y aristas
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
            //gridCells[pathPos].AddComponent<PathVisual>();
            pathPositions.Add(spawnPos);

            CrearCelda(pathPos); // Cada path es un vértice
            if (i > 0)
            {
                ConectarCeldas(startPath + rotatedOffsets[i - 1], pathPos, 1); // Conectar con anterior
            }
            else
            {
                ConectarCeldas(currentPathEnd, pathPos, 1); // Conectar el inicio con la primer celda path
            }
            prevPath = pathPos;
        }

        currentPathEnd = startPath + rotatedOffsets[^1];
        placedTiles.Add(new PlacedTileData(tile.tileName, bottomLeft));
    }

    // Obtener dirección del camino
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

    // Rotar offsets (según tu lógica actual)
    public List<Vector2Int> RotateOffsets(Vector2Int[] offsets, Vector2Int dir)
    {
        if (dir == Vector2Int.up) return offsets.ToList();
        if (dir == Vector2Int.right) return offsets.Select(o => new Vector2Int(o.y, -o.x)).ToList();
        if (dir == Vector2Int.down) return offsets.Select(o => new Vector2Int(-o.x, -o.y)).ToList();
        if (dir == Vector2Int.left) return offsets.Select(o => new Vector2Int(-o.y, o.x)).ToList();
        return offsets.ToList();
    }

    // TileOptions de ejemplo
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
            })
        };
        return tiles;
    }

    // Expansión random (filtros válidos a gusto)
    public void ApplyRandomValidTileExpansion()
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

        int randomIndex = UnityEngine.Random.Range(0, validTiles.Count);
        TileExpansion selectedTile = validTiles[randomIndex];

        Vector3 worldPosition = new Vector3(currentPathEnd.x * cellSize, 0, currentPathEnd.y * cellSize);

        ApplyTileExpansionAtWorldPosition(selectedTile, worldPosition);
    }

    // Ejemplo de adyacencias, igual que tu función original
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

        return disabledTiles;
    }
    public Vector3[] GetPathPositions()
    {
        return pathPositions.ToArray();
    }

    // Ejemplo: obtener camino entre dos posiciones usando el grafo
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
        var camino = ObtenerCaminoOptimo(inicio, fin); // el método con Dijkstra que ya te pasé
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

        // Buscar la posición del índice destino en Etiqs
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

        string pathStr = AlgDijkstra.nodos[idxDestino]; // ejemplo: "3,12,25,37"
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
        // Para debug: dibujar las conexiones del grafo
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
