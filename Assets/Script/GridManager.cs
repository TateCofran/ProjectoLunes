using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Script;   // <-- Tu namespace donde está GrafoMA
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int width = 5;
    public int height = 5;
    public float cellSize = 1f;

    public GameObject cellPrefab;
    public GameObject pathPrefab;
    public GameObject corePrefab;

    private const int WORLD_WIDTH = 1000;    // o cualquier ancho suficiente para todo tu mundo
    private GrafoMA grafo;

    public GameObject tilePreviewPrefab;
    private GameObject currentPreview;
    private List<Vector3> orderedPathPositions = new List<Vector3>();

    private Dictionary<Vector2Int, GameObject> gridCells = new();
    private List<Vector3> pathPositions = new();

    private Vector2Int currentPathEnd;
    private int tileCount = 0;

    private List<PlacedTileData> placedTiles = new();
    private Dictionary<Vector2Int, GameObject> occupiedPaths = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> occupiedCells = new Dictionary<Vector2Int, GameObject>();

    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    private void Awake()
    {
        grafo = new GrafoMA();
        grafo.InicializarGrafo();
    }
    IEnumerator Start()
    {
        GenerateInitialGrid();
        yield return new WaitForSeconds(0.1f); // pequeño delay para asegurar que la UI esté lista

    }

    void GenerateInitialGrid()
    {
        // Limpia cualquier estado previo si fuera necesario
        gridCells.Clear();
        pathPositions.Clear();
        tileCount = 0;

        // Creamos un objeto parent para el tile inicial
        GameObject tileCore = new GameObject("Tile-0");
        tileCore.transform.parent = this.transform;

        // 1) Instanciamos la grilla base de celdas
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                Vector3 worldPos = new Vector3(x * cellSize, 0f, z * cellSize);
                var cellGO = Instantiate(cellPrefab, worldPos, Quaternion.identity, tileCore.transform);
                gridCells[pos] = cellGO;
            }
        }

        // 2) Determinamos la posición del core: centro en X, borde inferior en Z (z = 0)
        Vector2Int coreGrid = new Vector2Int(width / 2, 0);
        Vector3 coreWorld = new Vector3(coreGrid.x * cellSize, 0f, coreGrid.y * cellSize);

        // 3) Removemos la celda de suelo bajo el core y colocamos el prefab del core
        if (gridCells.TryGetValue(coreGrid, out var oldCell))
        {
            Destroy(oldCell);
            gridCells.Remove(coreGrid);
        }
        Instantiate(corePrefab, coreWorld, Quaternion.identity, tileCore.transform);

        // 4) GRAFO: agregamos el vértice del core
        int idxCore = coreGrid.y * WORLD_WIDTH + coreGrid.x;
        grafo.AgregarVertice(idxCore);

        // 5) Generamos el camino inicial (recto hacia arriba) y añadimos vértices + aristas
        int prevIdx = idxCore;
        for (int z = 1; z < height; z++)
        {
            Vector2Int stepGrid = new Vector2Int(coreGrid.x, z);
            Vector3 stepWorld = new Vector3(stepGrid.x * cellSize, 0f, stepGrid.y * cellSize);

            // Reemplazamos la celda por un pathPrefab
            if (gridCells.TryGetValue(stepGrid, out var cellToRemove))
            {
                Destroy(cellToRemove);
                gridCells.Remove(stepGrid);
            }
            Instantiate(pathPrefab, stepWorld, Quaternion.identity, tileCore.transform);

            // Guardamos para visual y lógica de spawn
            pathPositions.Add(stepWorld);
            currentPathEnd = stepGrid;

            // GRAFO: agregamos vértice + arista desde el paso anterior
            int thisIdx = stepGrid.y * WORLD_WIDTH + stepGrid.x;
            grafo.AgregarVertice(thisIdx);
            grafo.AgregarArista(0, prevIdx, thisIdx, 1);
            prevIdx = thisIdx;
        }

        // 6) Marcamos que ya tenemos un tile completo
        tileCount = 1;
    }
    void AddTileToGraph(Vector2Int prevExitGlobal, List<Vector2Int> rotatedOffsets)
    {
        int prevIdx = prevExitGlobal.y * WORLD_WIDTH + prevExitGlobal.x;

        foreach (var local in rotatedOffsets)
        {
            Vector2Int g = prevExitGlobal + local;
            int idx = g.y * WORLD_WIDTH + g.x;
            grafo.AgregarVertice(idx);
            grafo.AgregarArista(0, prevIdx, idx, 1);
            prevIdx = idx;
        }
    }
    public List<TileExpansion> GetTileOptions()
    {
        List<TileExpansion> tiles = new()
    {
        // Recto: entra abajo centro, sale arriba centro
        new TileExpansion("Recto",
            new[] {
                new Vector2Int(2, 0),
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(2, 3),
                new Vector2Int(2, 4)
            },
            new Vector2Int(2, 0), // entrada
            new Vector2Int(2, 4)  // salida
        ),

        // L-Shape: entra abajo centro, sale derecha centro
        new TileExpansion("L-Shape",
            new[] {
                new Vector2Int(2, 0),
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(3, 2),
                new Vector2Int(4, 2)
            },
            new Vector2Int(2, 0), // entrada
            new Vector2Int(4, 2)  // salida
        ),

        // L-Inverso: entra abajo centro, sale izquierda centro
        new TileExpansion("L-Inverso",
            new[] {
                new Vector2Int(2, 0),
                new Vector2Int(2, 1),
                new Vector2Int(2, 2),
                new Vector2Int(1, 2),
                new Vector2Int(0, 2)
            },
            new Vector2Int(2, 0), // entrada
            new Vector2Int(0, 2)  // salida
        )
    };
        // ---- CRUZ: entrada y hasta 2 salidas random ----
        var bordesCentro = new List<Vector2Int>
    {
        new Vector2Int(2, 0), // abajo
        new Vector2Int(4, 2), // derecha
        new Vector2Int(2, 4), // arriba
        new Vector2Int(0, 2)  // izquierda
    };

        // Elegí entrada al azar
        int idxEntrada = Random.Range(0, bordesCentro.Count);
        Vector2Int entrada = bordesCentro[idxEntrada];

        // Salidas posibles (no la entrada)
        var posiblesSalidas = bordesCentro.Where((v, idx) => idx != idxEntrada).ToList();
        // Mezclar y elegir hasta 2 salidas
        posiblesSalidas = posiblesSalidas.OrderBy(x => Random.value).Take(2).ToList();

        // Calculá el centro
        Vector2Int centro = new Vector2Int(2, 2);

        // Offsets para la cruz: entrada?centro, y del centro?cada salida
        List<Vector2Int> offsets = new List<Vector2Int>();

        // Camino de entrada a centro
        Vector2Int dirEntrada = PasoUnitario(entrada, centro);
        for (Vector2Int pos = entrada; pos != centro; pos += dirEntrada)
            offsets.Add(pos);
        offsets.Add(centro);

        // Caminos centro a cada salida
        foreach (var salida in posiblesSalidas)
        {
            Vector2Int dirSalida = PasoUnitario(centro, salida);
            for (Vector2Int pos = centro + dirSalida; pos != salida + dirSalida; pos += dirSalida)
                offsets.Add(pos);
        }

        // TileExpansion de la cruz (primer salida como salida "principal")
        tiles.Add(new TileExpansion(
            "Cruz",
            offsets.ToArray(),
            entrada,
            posiblesSalidas[0] // usá la primera como salida principal
        ));

        return tiles;
    }

    void ExpandGridIfNeeded(Vector2Int position)
    {
        if (!gridCells.ContainsKey(position))
        {
            Vector3 pos = new Vector3(position.x * cellSize, 0, position.y * cellSize);
            gridCells[position] = Instantiate(cellPrefab, pos, Quaternion.identity, transform);

        }
    }

    public void ApplyTileExpansionAtWorldPosition(TileExpansion tile, Vector3 _unused)
    {
        Vector2Int lastGridPos = GetLastPathGridPosition();

        // Direcciones válidas para rotar
        List<Vector2Int> directions = new List<Vector2Int> {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

        List<Vector2Int> rotatedOffsets = null;
        Vector2Int rotatedEntrada = Vector2Int.zero;
        Vector2Int rotatedSalida = Vector2Int.zero;
        Vector2Int finalTileBottomLeft = Vector2Int.zero;
        Vector2Int usedDir = Vector2Int.zero;
        bool found = false;

        foreach (var dir in directions)
        {
            // Rotar offsets y entrada/salida
            var rof = RotateOffsets(tile.pathOffsets, dir);
            var ent = RotateSingleOffset(tile.entrada, dir);
            var sal = RotateSingleOffset(tile.salida, dir);

            // Base de tile para que entrada global calce justo en el extremo del camino anterior
            Vector2Int tileBottomLeft = lastGridPos - ent;

            // 1) Validar que la entrada calce exactamente con el extremo del camino
            bool conectaBien = (tileBottomLeft + ent == lastGridPos);

            // 2) Validar que ninguna celda (ni path ni suelo) esté ocupada
            bool tileOcupado = false;
            for (int y = 0; y < tile.tileSize.y; y++)
            {
                for (int x = 0; x < tile.tileSize.x; x++)
                {
                    Vector2Int cellLocal = RotateSingleOffset(new Vector2Int(x, y), dir);
                    Vector2Int cellWorld = tileBottomLeft + cellLocal;
                    if (occupiedPositions.Contains(cellWorld))
                    {
                        tileOcupado = true;
                        break;
                    }
                }
                if (tileOcupado) break;
            }

            // Debug opcional
            Debug.Log($"[TilePlacement] Dir: {dir}, TileBase: {tileBottomLeft}, ConectaBien: {conectaBien}, TileOcupado: {tileOcupado}");

            if (conectaBien && !tileOcupado)
            {
                rotatedOffsets = rof;
                rotatedEntrada = ent;
                rotatedSalida = sal;
                finalTileBottomLeft = tileBottomLeft;
                usedDir = dir;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogError("[GridManager] No se pudo encontrar una rotación válida para el tile. Abortando.");
            return;
        }

        // Instanciar visualmente el tile en el lugar calculado
        Vector3 tileParentWorldPos = new Vector3(
            finalTileBottomLeft.x * cellSize,
            0,
            finalTileBottomLeft.y * cellSize
        );
        GameObject tileParent = new GameObject($"Tile-{tileCount}");
        tileParent.transform.parent = this.transform;
        tileParent.transform.position = tileParentWorldPos;

        // Instanciar path (y marcar posiciones ocupadas)
        var pathSet = new HashSet<Vector2Int>(rotatedOffsets);
        foreach (var local in rotatedOffsets)
        {
            Vector3 worldPos = new Vector3(local.x * cellSize, 0, local.y * cellSize) + tileParentWorldPos;
            Instantiate(pathPrefab, worldPos, Quaternion.identity, tileParent.transform);
            pathPositions.Add(worldPos);
            occupiedPositions.Add(finalTileBottomLeft + local); // Marcar como ocupado
        }

        // Instanciar celdas restantes (no-path, rotadas, y marcar ocupadas)
        for (int y = 0; y < tile.tileSize.y; y++)
        {
            for (int x = 0; x < tile.tileSize.x; x++)
            {
                Vector2Int local = RotateSingleOffset(new Vector2Int(x, y), usedDir);
                if (!pathSet.Contains(local))
                {
                    Vector3 worldPos = new Vector3(local.x * cellSize, 0, local.y * cellSize) + tileParentWorldPos;
                    Instantiate(cellPrefab, worldPos, Quaternion.identity, tileParent.transform);
                    occupiedPositions.Add(finalTileBottomLeft + local); // Marcar como ocupado
                }
            }
        }

        // Estado global: el extremo final del path para el próximo tile
        currentPathEnd = finalTileBottomLeft + rotatedSalida;
        placedTiles.Add(new PlacedTileData(tile.tileName, finalTileBottomLeft));
        tileCount++;
    }
    public void ExpandPathWithRandomTile()
    {
        // 1. Elegí el tipo de tile que quieras (acá es aleatorio)
        List<TileExpansion> options = GetTileOptions(); // O tu propio método de selección
        TileExpansion selected = options[UnityEngine.Random.Range(0, options.Count)];

        // 2. Obtené la posición donde debe colocarse el tile (en general, el offset correcto)
        Vector3 lastWorldPos = GetLastPathWorldPosition();

        // 3. Expandí el camino (si worldPosition es necesario en tu lógica de preview, pasalo)
        ApplyTileExpansionAtWorldPosition(selected, lastWorldPos);
    }

    private List<Vector2Int> RotateOffsets(List<Vector2Int> original, Vector2Int direction)
    {
        List<Vector2Int> rotated = new();

        foreach (var offset in original)
        {
            Vector2Int rotatedOffset = offset;

            if (direction == Vector2Int.right)
            {
                rotatedOffset = new Vector2Int(offset.y, -offset.x);
            }
            else if (direction == Vector2Int.down)
            {
                rotatedOffset = new Vector2Int(-offset.x, -offset.y);
            }
            else if (direction == Vector2Int.left)
            {
                rotatedOffset = new Vector2Int(-offset.y, offset.x);
            }
            // if direction == up, keep original

            rotated.Add(rotatedOffset);
        }

        return rotated;
    }

    private Vector2Int RotateSingleOffset(Vector2Int offset, Vector2Int direction)
    {
        if (direction == Vector2Int.right)
            return new Vector2Int(offset.y, -offset.x);
        if (direction == Vector2Int.down)
            return new Vector2Int(-offset.x, -offset.y);
        if (direction == Vector2Int.left)
            return new Vector2Int(-offset.y, offset.x);
        // Vector2Int.up o cualquier otra -> sin rotar
        return offset;
    }
    public bool IsTileAtPosition(Vector2Int pos)
    {
        return placedTiles.Any(p => p.basePosition == pos);
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // --- Mostrar tiles ya colocados (cyan) ---
        Gizmos.color = Color.cyan;
        foreach (var tile in placedTiles)
        {
            Vector3 center = new Vector3((tile.basePosition.x + 2) * cellSize, 0, (tile.basePosition.y + 2) * cellSize);
            Gizmos.DrawWireCube(center + Vector3.up * 0.1f, new Vector3(5 * cellSize, 0.1f, 5 * cellSize));
            UnityEditor.Handles.Label(center + Vector3.up * 0.3f, tile.name);
        }

        // --- Mostrar adyacentes ocupados (rojo) ---
        Vector2Int[] directions = new[]
        {
        Vector2Int.up * 5,
        Vector2Int.down * 5,
        Vector2Int.left * 5,
        Vector2Int.right * 5
    };

        HashSet<Vector2Int> allTilePositions = new(placedTiles.Select(p => p.basePosition));

        foreach (var tile in placedTiles)
        {
            foreach (var dir in directions)
            {
                Vector2Int adjacent = tile.basePosition + dir;
                if (!allTilePositions.Contains(adjacent)) continue;

                Vector3 center = new Vector3((adjacent.x + 2) * cellSize, 0, (adjacent.y + 2) * cellSize);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center + Vector3.up * 0.05f, new Vector3(5 * cellSize, 0.1f, 5 * cellSize));
            }
        }

        // --- Tile siguiente (azul) ---
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

        Vector3 blueCenter = new Vector3(
            (nextBottomLeft.x + 2) * cellSize,
            0,
            (nextBottomLeft.y + 2) * cellSize
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(blueCenter + Vector3.up * 0.15f, new Vector3(5 * cellSize, 0.1f, 5 * cellSize));

        // --- Adyacentes del próximo tile (naranja/violeta) con tile sugerido ---
        Vector2Int[] localDirs = new[]
        {
        Vector2Int.up * 5,
        Vector2Int.down * 5,
        Vector2Int.left * 5,
        Vector2Int.right * 5
    };

        foreach (var dir in localDirs)
        {
            Vector2Int adjacent = nextBottomLeft + dir;

            // No mostrar si es de donde vino el camino
            if (adjacent == currentPathEnd)
                continue;

            Vector3 center = new Vector3((adjacent.x + 2) * cellSize, 0, (adjacent.y + 2) * cellSize);

            // Color según si hay tile en esa posición
            Gizmos.color = allTilePositions.Contains(adjacent)
                ? new Color(0.6f, 0f, 0.8f)   //  Violeta (ocupado)
                : new Color(1f, 0.5f, 0f);    //  Naranja (libre)

            Gizmos.DrawWireCube(center + Vector3.up * 0.05f, new Vector3(5 * cellSize, 0.1f, 5 * cellSize));

            // --- Mostrar nombre del tile sugerido ---
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

            if (!string.IsNullOrEmpty(sugerido))
                UnityEditor.Handles.Label(center + Vector3.up * 0.2f, sugerido);
        }
    }
#endif

    Vector2Int GetPathDirection()
    {
        if (pathPositions.Count < 2) return Vector2Int.up;
        var a = pathPositions[^2];
        var b = pathPositions[^1];
        return new Vector2Int(
            Mathf.RoundToInt(b.x / cellSize) - Mathf.RoundToInt(a.x / cellSize),
            Mathf.RoundToInt(b.z / cellSize) - Mathf.RoundToInt(a.z / cellSize)
        );
    }
    public Vector2Int GetLastPathGridPosition()
    {
        if (pathPositions.Count == 0)
            return Vector2Int.zero;

        Vector3 lastWorld = pathPositions[^1];
        return new Vector2Int(
            Mathf.RoundToInt(lastWorld.x / cellSize),
            Mathf.RoundToInt(lastWorld.z / cellSize)
        );
    }

    public Vector3[] GetPathPositions()
    {
        return pathPositions.ToArray();
    }

    Vector3 GetLastPathWorldPosition()
    {
        if (pathPositions.Count > 0)
            return pathPositions[^1];
        return Vector3.zero;
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

    private Vector2Int PasoUnitario(Vector2Int desde, Vector2Int hasta)
    {
        int dx = Mathf.Clamp(hasta.x - desde.x, -1, 1);
        int dy = Mathf.Clamp(hasta.y - desde.y, -1, 1);
        return new Vector2Int(dx, dy);
    }
}

[System.Serializable]
public class TileExpansion
{
    public string tileName;
    public Vector2Int tileSize = new Vector2Int(5, 5);
    public List<Vector2Int> pathOffsets; // Path principal (único para recto/L)
    public Vector2Int entrada;
    public Vector2Int salida;

    // Nuevo: lista de ramas para tiles complejos
    public List<List<Vector2Int>> pathBranches;

    // Constructor simple (Recto, L, etc.)
    public TileExpansion(string name, Vector2Int[] offsets, Vector2Int entrada, Vector2Int salida)
    {
        tileName = name;
        pathOffsets = new List<Vector2Int>(offsets);
        this.entrada = entrada;
        this.salida = salida;
        pathBranches = null; // Por defecto, no ramas extra
    }

    // Constructor para tiles múltiples ramas (ej: Cruz)
    public TileExpansion(string name, List<List<Vector2Int>> branches)
    {
        tileName = name;
        pathBranches = branches;
        pathOffsets = branches[0]; // Por default, usa la primera rama
        // Opcional: podrías definir entrada/salida, o calcularlas según la rama activa
    }

    public static TileExpansion Cruz()
    {
        var branches = new List<List<Vector2Int>>();

        // Ramas de la cruz (conexiones posibles: cada entrada a cada salida)
        branches.Add(new List<Vector2Int> { new Vector2Int(2, 0), new Vector2Int(2, 1), new Vector2Int(2, 2), new Vector2Int(2, 3), new Vector2Int(2, 4) }); // abajo-arriba
        branches.Add(new List<Vector2Int> { new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(4, 2) }); // izq-der
                                                                                                                                                             // Si querés podés agregar más ramas, o sólo estas dos si nunca hay dos entradas activas a la vez

        return new TileExpansion("Cruz", branches);
    }

}

[System.Serializable]
public class PlacedTileData
{
    public string name;
    public Vector2Int basePosition;

    public PlacedTileData(string name, Vector2Int basePosition)
    {
        this.name = name;
        this.basePosition = basePosition;
    }
}