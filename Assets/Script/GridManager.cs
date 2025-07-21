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
    }

    // NUEVO: Manejar el tile circular
    if (tile.tileName == "Circular-Redoma")
    {
        Debug.Log("[GridManager] Creando tile circular tipo redoma");
        CrearRedomaCircular(startPath, rotatedOffsets, pathDirection, tileParent);
    }
    else
    {
        currentPathEnd = startPath + rotatedOffsets[^1];
    }

    placedTiles.Add(new PlacedTileData(tile.tileName, bottomLeft));
}
 private void CrearRedomaCircular(Vector2Int startPath, List<Vector2Int> mainOffsets, Vector2Int mainDirection, GameObject tileParent)
{
    Debug.Log($"[GridManager] Creando redoma circular en dirección: {mainDirection}");
    
    // Puntos clave del tile
    Vector2Int puntoEntrada = startPath + mainOffsets[1];     // donde entra
    Vector2Int puntoDivision = startPath + mainOffsets[2];    // donde se divide el camino
    Vector2Int puntoReunion = startPath + mainOffsets[3];     // donde se reúne
    Vector2Int puntoSalida = startPath + mainOffsets[4];      // donde sale
    
    Debug.Log($"[GridManager] Puntos: Entrada={puntoEntrada}, División={puntoDivision}, Reunión={puntoReunion}, Salida={puntoSalida}");
    
    // 1. CREAR ATAJO CENTRAL (peso 1 - óptimo para Dijkstra)
    CrearAtajoCentral(puntoDivision, puntoReunion, tileParent);
    
    // 2. CREAR CAMINO CIRCULAR (peso 3 - menos eficiente, para BFS)
    CrearCaminoCircularRedoma(puntoDivision, puntoReunion, mainDirection, tileParent);
    
    // 3. Actualizar el punto final
    currentPathEnd = puntoSalida;
    
    Debug.Log($"[GridManager] Redoma circular creada exitosamente. Nuevo endpoint: {currentPathEnd}");
}

// Crear el atajo central directo
private void CrearAtajoCentral(Vector2Int desde, Vector2Int hasta, GameObject parent)
{
    Debug.Log($"[GridManager] Creando atajo central desde {desde} hasta {hasta}");
    
    // Conexión directa con peso 1 (más eficiente para Dijkstra)
    ConectarCeldas(desde, hasta, 1);
    
    Debug.Log($"[GridManager] Atajo central creado con peso 1");
}

// Crear el camino circular (la "redoma")
private void CrearCaminoCircularRedoma(Vector2Int desde, Vector2Int hasta, Vector2Int mainDirection, GameObject parent)
{
    Debug.Log($"[GridManager] Creando camino circular tipo redoma desde {desde} hasta {hasta}");
    
    // Determinar direcciones laterales según la dirección principal
    Vector2Int direccionIzquierda, direccionDerecha;
    
    if (mainDirection == Vector2Int.up)
    {
        direccionIzquierda = Vector2Int.left;
        direccionDerecha = Vector2Int.right;
    }
    else if (mainDirection == Vector2Int.right)
    {
        direccionIzquierda = Vector2Int.up;
        direccionDerecha = Vector2Int.down;
    }
    else if (mainDirection == Vector2Int.left)
    {
        direccionIzquierda = Vector2Int.down;
        direccionDerecha = Vector2Int.up;
    }
    else
    {
        // Fallback
        direccionIzquierda = Vector2Int.left;
        direccionDerecha = Vector2Int.right;
    }
    
    // LADO IZQUIERDO de la redoma
    CrearLadoRedoma(desde, hasta, direccionIzquierda, mainDirection, parent, "izquierdo");
    
    // LADO DERECHO de la redoma (solo si no va hacia abajo)
    if (direccionDerecha != Vector2Int.down || mainDirection == Vector2Int.up)
    {
        CrearLadoRedoma(desde, hasta, direccionDerecha, mainDirection, parent, "derecho");
    }
}

// Crear un lado de la redoma (izquierdo o derecho)
private void CrearLadoRedoma(Vector2Int desde, Vector2Int hasta, Vector2Int direccionLateral, Vector2Int direccionPrincipal, GameObject parent, string lado)
{
    Debug.Log($"[GridManager] Creando lado {lado} de la redoma");
    
    // Calcular puntos del arco
    Vector2Int punto1 = desde + direccionLateral;                                    // primer punto lateral
    Vector2Int punto2 = desde + direccionLateral * 2;                               // punto más alejado lateralmente
    Vector2Int punto3 = punto2 + direccionPrincipal * (hasta.y - desde.y);         // avanzar hacia adelante
    Vector2Int punto4 = punto3 - direccionLateral;                                  // volver hacia el centro
    Vector2Int punto5 = punto4 - direccionLateral;                                  // punto antes de la reunión
    
    List<Vector2Int> puntosArco = new List<Vector2Int> { desde, punto1, punto2, punto3, punto4, punto5, hasta };
    
    // Crear y conectar cada punto del arco
    Vector2Int puntoAnterior = desde;
    for (int i = 1; i < puntosArco.Count; i++)
    {
        Vector2Int puntoActual = puntosArco[i];
        
        // Solo crear si está dentro del mapa y no va hacia abajo
        if (!EstaFueraDelMapa(puntoActual) && puntoActual.y >= desde.y)
        {
            // Solo crear celda si no es el punto de reunión (ya existe)
            if (puntoActual != hasta && puntoActual != desde)
            {
                Vector3 worldPos = new Vector3(puntoActual.x * cellSize, 0, puntoActual.y * cellSize);
                CrearCeldaDeCamino(puntoActual, worldPos, parent);
            }
            
            // Conectar con peso 3 (menos eficiente que el atajo central)
            ConectarCeldas(puntoAnterior, puntoActual, 3);
            puntoAnterior = puntoActual;
        }
        else
        {
            Debug.LogWarning($"[GridManager] Punto {puntoActual} del lado {lado} está fuera del mapa o va hacia abajo");
            break;
        }
    }
    
    Debug.Log($"[GridManager] Lado {lado} de la redoma creado con peso 3");
}

// Función auxiliar para crear celdas de camino
private void CrearCeldaDeCamino(Vector2Int pos, Vector3 worldPos, GameObject parent)
{
    // Destruir celda existente si la hay
    if (gridCells.ContainsKey(pos))
    {
        Destroy(gridCells[pos]);
        gridCells.Remove(pos);
    }
    
    // Crear nueva celda de camino
    gridCells[pos] = Instantiate(pathPrefab, worldPos, Quaternion.identity, parent.transform);
    pathPositions.Add(worldPos);
    
    // Agregar al grafo
    CrearCelda(pos);
}

// Función auxiliar para verificar límites
private bool EstaFueraDelMapa(Vector2Int pos)
{
    return pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height;
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

            // NUEVO: Tile Circular con atajo central (reemplaza T-Bifurcacion)
            new TileExpansion("Circular-Redoma", new[]
            {
                new Vector2Int(0, 0),  // entrada
                Vector2Int.up,         // primer paso
                Vector2Int.up * 2,     // punto de división
                Vector2Int.up * 3,     // punto de reunión (atajo central)
                Vector2Int.up * 4      // salida
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

        // CAMBIO: Forzar redoma circular cada 2 oleadas (oleadas 2, 4, 6, etc)
        bool shouldForceCircular = (waveNumber > 1 && waveNumber % 2 == 0);

        if (shouldForceCircular && validTiles.Any(t => t.tileName == "Circular-Redoma"))
        {
            selectedTile = validTiles.First(t => t.tileName == "Circular-Redoma");
            Debug.Log($"[GridManager] FORZANDO REDOMA CIRCULAR en oleada {waveNumber}");
        }
        else
        {
            // Excluir la redoma circular de la selección aleatoria en oleadas impares
            List<TileExpansion> tilesWithoutCircular = validTiles
                .Where(t => t.tileName != "Circular-Redoma")
                .ToList();
        
            if (tilesWithoutCircular.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, tilesWithoutCircular.Count);
                selectedTile = tilesWithoutCircular[randomIndex];
            }
            else
            {
                selectedTile = validTiles[0];
            }
        }

        Debug.Log($"[GridManager] Tile seleccionado: {selectedTile.tileName} para oleada {waveNumber}");

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
    else if (pathDir == Vector2Int.left)
        tileOffset = new Vector2Int(-5, -5 / 2);

    Vector2Int nextBottomLeft = GetLastPathGridPosition() + tileOffset;

    Vector2Int[] localDirs = new[]
    {
        Vector2Int.up * 5,
        Vector2Int.left * 5,
        Vector2Int.right * 5
        // Removido Vector2Int.down para evitar ir hacia abajo
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
            (pathDir == Vector2Int.right && delta == Vector2Int.up * 5) ||
            (pathDir == Vector2Int.left && delta == Vector2Int.up * 5))
            sugerido = "L-Shape";
        else if (
            (pathDir == Vector2Int.up && delta == Vector2Int.left * 5) ||
            (pathDir == Vector2Int.left && delta == Vector2Int.up * 5) ||
            (pathDir == Vector2Int.right && delta == Vector2Int.up * 5))
            sugerido = "L-Inverso";

        if (!string.IsNullOrEmpty(sugerido) && !disabledTiles.Contains(sugerido))
            disabledTiles.Add(sugerido);
    }

    // La redoma circular siempre está disponible (no se deshabilita por adyacencia)
    disabledTiles.Remove("Circular-Redoma");
    
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



    // Devuelve una lista de Vector2Int con todas las celdas ocupadas por torretas.
    public List<Vector2Int> ObtenerCeldasConTorretas()
    {
        List<Vector2Int> bloqueadas = new List<Vector2Int>();
        foreach (var par in gridCells)
        {
            var cell = par.Value;
            // Cambia este if según tu lógica para detectar una torreta en esa celda
            if (cell.GetComponent<Turret>() != null)  // o cualquier lógica que uses
                bloqueadas.Add(par.Key);
        }
        return bloqueadas;
    }


    public Vector3[] ObtenerCaminoEvitarTorretas(Vector2Int inicio, Vector2Int fin, List<Vector2Int> celdasBloqueadas)
    {
        // BFS modificado que saltea los nodos bloqueados
        if (!celdaToVertice.ContainsKey(inicio) || !celdaToVertice.ContainsKey(fin))
            return null;

        var visitados = new HashSet<int>();
        var anterior = new Dictionary<int, int>();
        var cola = new Queue<int>();

        int vInicio = celdaToVertice[inicio];
        int vFin = celdaToVertice[fin];
        visitados.Add(vInicio);
        cola.Enqueue(vInicio);

        // Precalcular los vértices bloqueados
        var verticesBloqueados = new HashSet<int>(
            celdasBloqueadas.Where(c => celdaToVertice.ContainsKey(c)).Select(c => celdaToVertice[c])
        );

        while (cola.Count > 0)
        {
            int actual = cola.Dequeue();
            if (actual == vFin) break;

            for (int i = 0; i < grafo.cantNodos; i++)
            {
                if (grafo.ExisteArista(actual, i) && !visitados.Contains(i) && !verticesBloqueados.Contains(i))
                {
                    visitados.Add(i);
                    anterior[i] = actual;
                    cola.Enqueue(i);
                }
            }
        }

        // Reconstruir el camino
        if (!visitados.Contains(vFin))
            return null;

        List<int> camino = new List<int>();
        int temp = vFin;
        while (temp != vInicio)
        {
            camino.Add(temp);
            temp = anterior[temp];
        }
        camino.Add(vInicio);
        camino.Reverse();

        // Convertir a posiciones en el mundo
        var path = camino.Select(id => celdaToVertice.First(x => x.Value == id).Key)
                         .Select(pos => new Vector3(pos.x * cellSize, 0, pos.y * cellSize))
                         .ToArray();

        return path;
    }



}
