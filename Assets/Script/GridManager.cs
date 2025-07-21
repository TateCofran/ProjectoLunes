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
    public int tileCount = 0;

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

    
        Vector2Int corePos = new Vector2Int(width / 2, 0);
        Vector3 coreWorldPos = new Vector3(corePos.x * cellSize, 0, corePos.y * cellSize);

      
        if (gridCells.ContainsKey(corePos))
        {
            Destroy(gridCells[corePos]);
            gridCells.Remove(corePos);
        }

       
        Instantiate(corePrefab, coreWorldPos, Quaternion.identity, tileCore.transform);
        CrearCelda(corePos); 

        pathPositions.Add(coreWorldPos);
        currentPathEnd = corePos;

       
        Vector2Int initialBottomLeft = new Vector2Int(corePos.x - 2, 0);
        placedTiles.Add(new PlacedTileData("Inicial", initialBottomLeft));

       
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

  
    if (tile.tileName == "Circular-Redoma")
    {
       
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
    
    Vector2Int puntoEntrada = startPath + mainOffsets[1];     
    Vector2Int puntoDivision = startPath + mainOffsets[2];   
    Vector2Int puntoReunion = startPath + mainOffsets[3];    
    Vector2Int puntoSalida = startPath + mainOffsets[4];
    
    Debug.Log($"[GridManager] Puntos: Entrada={puntoEntrada}, División={puntoDivision}, Reunión={puntoReunion}, Salida={puntoSalida}");
    
    
    CrearAtajoCentral(puntoDivision, puntoReunion, tileParent);
    
    CrearCaminoCircularRedoma(puntoDivision, puntoReunion, mainDirection, tileParent);
    

    currentPathEnd = puntoSalida;
    
  
}


private void CrearAtajoCentral(Vector2Int desde, Vector2Int hasta, GameObject parent)
{
    ConectarCeldas(desde, hasta, 1);
}


private void CrearCaminoCircularRedoma(Vector2Int desde, Vector2Int hasta, Vector2Int mainDirection, GameObject parent)
{
    Debug.Log($"[GridManager] Creando camino circular tipo redoma desde {desde} hasta {hasta}");
    
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
        
        direccionIzquierda = Vector2Int.left;
        direccionDerecha = Vector2Int.right;
    }
    
   
    CrearLadoRedoma(desde, hasta, direccionIzquierda, mainDirection, parent, "izquierdo");
    
   
    if (direccionDerecha != Vector2Int.down || mainDirection == Vector2Int.up)
    {
        CrearLadoRedoma(desde, hasta, direccionDerecha, mainDirection, parent, "derecho");
    }
}


private void CrearLadoRedoma(Vector2Int desde, Vector2Int hasta, Vector2Int direccionLateral, Vector2Int direccionPrincipal, GameObject parent, string lado)
{
   
    Vector2Int punto1 = desde + direccionLateral;                                   
    Vector2Int punto2 = desde + direccionLateral * 2;                              
    Vector2Int punto3 = punto2 + direccionPrincipal * (hasta.y - desde.y);         
    Vector2Int punto4 = punto3 - direccionLateral;                                
    Vector2Int punto5 = punto4 - direccionLateral;                                 
    
    List<Vector2Int> puntosArco = new List<Vector2Int> { desde, punto1, punto2, punto3, punto4, punto5, hasta };
    
  
    Vector2Int puntoAnterior = desde;
    for (int i = 1; i < puntosArco.Count; i++)
    {
        Vector2Int puntoActual = puntosArco[i];
        
      
        if (!EstaFueraDelMapa(puntoActual) && puntoActual.y >= desde.y)
        {
          
            if (puntoActual != hasta && puntoActual != desde)
            {
                Vector3 worldPos = new Vector3(puntoActual.x * cellSize, 0, puntoActual.y * cellSize);
                CrearCeldaDeCamino(puntoActual, worldPos, parent);
            }
            
          
            ConectarCeldas(puntoAnterior, puntoActual, 3);
            puntoAnterior = puntoActual;
        }
        else
        {
            Debug.LogWarning($"[GridManager] Punto {puntoActual} del lado {lado} está fuera del mapa o va hacia abajo");
            break;
        }
    }
    
  
}


private void CrearCeldaDeCamino(Vector2Int pos, Vector3 worldPos, GameObject parent)
{
 
    if (gridCells.ContainsKey(pos))
    {
        Destroy(gridCells[pos]);
        gridCells.Remove(pos);
    }
    

    gridCells[pos] = Instantiate(pathPrefab, worldPos, Quaternion.identity, parent.transform);
    pathPositions.Add(worldPos);
    
 
    CrearCelda(pos);
}


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
        
     
        int conexiones = 0;
        for (int i = 0; i < grafo.cantNodos; i++)
        {
            if (i != vertice && grafo.ExisteArista(vertice, i))
                conexiones++;
        }
        
       
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

       
            new TileExpansion("Circular-Redoma", new[]
            {
                new Vector2Int(0, 0),  
                Vector2Int.up,        
                Vector2Int.up * 2,    
                Vector2Int.up * 3,     
                Vector2Int.up * 4     
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
            return;
        }

        TileExpansion selectedTile;

       
        bool shouldForceCircular = (waveNumber > 1 && waveNumber % 2 == 0);

        if (shouldForceCircular && validTiles.Any(t => t.tileName == "Circular-Redoma"))
        {
            selectedTile = validTiles.First(t => t.tileName == "Circular-Redoma");
        }
        else
        {
          
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

        

        // Ejecutar Dijkstra
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

      

        
        List<Vector2Int> camino = new List<Vector2Int>();
        foreach (var idStr in idsStr)
        {
            if (int.TryParse(idStr, out int id))
            {
                Vector2Int pos = celdaToVertice.FirstOrDefault(x => x.Value == id).Key;
                if (!pos.Equals(default(Vector2Int))) camino.Add(pos);
            }
        }
        
        return camino;
    }
   
    void OnDrawGizmos()
    {
        
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



  
    public List<Vector2Int> ObtenerCeldasConTorretas()
    {
        List<Vector2Int> bloqueadas = new List<Vector2Int>();
        foreach (var par in gridCells)
        {
            var cell = par.Value;
           
            if (cell.GetComponent<Turret>() != null)  
                bloqueadas.Add(par.Key);
        }
        return bloqueadas;
    }

public Vector3[] ObtenerCaminoNormalWorld(Vector2Int inicio, Vector2Int fin)
{
    
    
    var camino = ObtenerCaminoBFS(inicio, fin);
    if (camino == null || camino.Count == 0)
    {
        if (camino == null)
        {
            return null;
        }
    }
    
 
    return camino.Select(pos => new Vector3(pos.x * cellSize, 0, pos.y * cellSize)).ToArray();
}


private List<Vector2Int> ObtenerCaminoBFS(Vector2Int inicio, Vector2Int fin)
{
    if (!celdaToVertice.ContainsKey(inicio) || !celdaToVertice.ContainsKey(fin))
        return null;

    int vInicio = celdaToVertice[inicio];
    int vFin = celdaToVertice[fin];

  

    var visitados = new HashSet<int>();
    var anterior = new Dictionary<int, int>();
    var cola = new Queue<int>();

    visitados.Add(vInicio);
    cola.Enqueue(vInicio);

    while (cola.Count > 0)
    {
        int actual = cola.Dequeue();
        if (actual == vFin) break;

       
        List<(int vertice, int peso)> conexiones = new List<(int, int)>();
        
        for (int i = 0; i < grafo.cantNodos; i++)
        {
            if (grafo.ExisteArista(actual, i) && !visitados.Contains(i))
            {
                int pesoArista = ObtenerPesoAristaEstimado(actual, i);
                conexiones.Add((i, pesoArista));
            }
        }

       
        conexiones.Sort((a, b) => b.peso.CompareTo(a.peso));

     
        foreach (var (vecino, peso) in conexiones)
        {
            if (!visitados.Contains(vecino))
            {
                visitados.Add(vecino);
                anterior[vecino] = actual;
                cola.Enqueue(vecino);
                
               
                Vector2Int posVecino = celdaToVertice.FirstOrDefault(x => x.Value == vecino).Key;
               
            }
        }
    }


    if (!visitados.Contains(vFin))
    {
        
        return null;
    }

    List<int> caminoVertices = new List<int>();
    int temp = vFin;
    while (temp != vInicio)
    {
        caminoVertices.Add(temp);
        temp = anterior[temp];
    }
    caminoVertices.Add(vInicio);
    caminoVertices.Reverse();

   
    List<Vector2Int> camino = new List<Vector2Int>();
    foreach (int vertice in caminoVertices)
    {
        var kvp = celdaToVertice.FirstOrDefault(x => x.Value == vertice);
        if (!kvp.Equals(default(KeyValuePair<Vector2Int, int>)))
        {
            camino.Add(kvp.Key);
        }
    }
    
    
    return camino;
}


private int ObtenerPesoAristaEstimado(int desde, int hacia)
{
    try
    {
        Vector2Int posDesde = celdaToVertice.FirstOrDefault(x => x.Value == desde).Key;
        Vector2Int posHacia = celdaToVertice.FirstOrDefault(x => x.Value == hacia).Key;
        
      
        float distancia = Vector2Int.Distance(posDesde, posHacia);
        
       
        if (distancia <= 1.1f)
        {
            
            Vector2Int corePos = new Vector2Int(width / 2, 0);
            Vector2Int direccion = posHacia - posDesde;
            
           
            if (Mathf.Abs(direccion.x) == 0 && direccion.y != 0)
            {
                return 1; 
            }
            else
            {
                return 2; 
            }
        }
        
        else
        {
            return 3; 
        }
    }
    catch
    {
        return 2; 
    }
}
    public Vector3[] ObtenerCaminoEvitarTorretas(Vector2Int inicio, Vector2Int fin, List<Vector2Int> celdasBloqueadas)
    {
       
        if (!celdaToVertice.ContainsKey(inicio) || !celdaToVertice.ContainsKey(fin))
            return null;

        var visitados = new HashSet<int>();
        var anterior = new Dictionary<int, int>();
        var cola = new Queue<int>();

        int vInicio = celdaToVertice[inicio];
        int vFin = celdaToVertice[fin];
        visitados.Add(vInicio);
        cola.Enqueue(vInicio);

     
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

     
        var path = camino.Select(id => celdaToVertice.First(x => x.Value == id).Key)
                         .Select(pos => new Vector3(pos.x * cellSize, 0, pos.y * cellSize))
                         .ToArray();

        return path;
    }


    public Vector3[] ObtenerCaminoMasLargo(Vector2Int inicio, Vector2Int fin)
    {
        if (!celdaToVertice.ContainsKey(inicio) || !celdaToVertice.ContainsKey(fin))
            return null;

        int vInicio = celdaToVertice[inicio];
        int vFin = celdaToVertice[fin];

        List<int> mejorCamino = new List<int>();
        List<int> caminoActual = new List<int>();
        HashSet<int> visitados = new HashSet<int>();

        void DFS(int actual)
        {
            caminoActual.Add(actual);
            visitados.Add(actual);

            if (actual == vFin)
            {
                if (caminoActual.Count > mejorCamino.Count)
                    mejorCamino = new List<int>(caminoActual);
            }
            else
            {
                for (int i = 0; i < grafo.cantNodos; i++)
                {
                    if (grafo.ExisteArista(actual, i) && !visitados.Contains(i))
                        DFS(i);
                }
            }

            caminoActual.RemoveAt(caminoActual.Count - 1);
            visitados.Remove(actual);
        }

        DFS(vInicio);

        if (mejorCamino.Count == 0)
            return null;

        // Convertir indices a posiciones en mundo
        var path = mejorCamino.Select(id => celdaToVertice.First(x => x.Value == id).Key)
                              .Select(pos => new Vector3(pos.x * cellSize, 0, pos.y * cellSize))
                              .ToArray();
        return path;
    }

}
