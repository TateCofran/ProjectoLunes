using Assets.Script;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public int tileSize = 5;
    public float cellSize = 1f;

    public GameObject cellPrefab;
    public GameObject pathPrefab;
    public GameObject corePrefab;

    private GrafoMA grafo;
    private List<List<int>> tilePaths = new List<List<int>>(); // Lista de caminos de cada tile
    private int tileCount = 0;

    private Vector3 lastExitWorldPos = Vector3.zero;
    private Vector2Int lastExitLocal = Vector2Int.zero; // salida en grid local

    public List<TileShape> formasDeTile = new List<TileShape>();
    private List<Vector3> pathEntrancesWorld = new List<Vector3>();
    // En GridManager
    public Vector3 lastTileEntryWorldPos; // La entrada del último tile agregado
    private List<Vector3> tileOffsets = new List<Vector3>();

    void Awake()
    {
        // Forma recto vertical (centro arriba a centro abajo)
        var caminoRecto = new List<Vector2Int>();
        for (int y = 0; y < tileSize; y++) caminoRecto.Add(new Vector2Int(tileSize / 2, y));
        formasDeTile.Add(new TileShape(
            "Recto",
            caminoRecto,
            new Vector2Int(tileSize / 2, 0),
            new List<Vector2Int> { new Vector2Int(tileSize / 2, tileSize - 1) },
            new List<(Vector2Int, Vector2Int)>()
        ));

        // Forma L (centro arriba, va hasta la mitad, gira a la derecha)
        var caminoL = new List<Vector2Int>();
        for (int y = 0; y <= tileSize / 2; y++) caminoL.Add(new Vector2Int(tileSize / 2, y));
        for (int x = tileSize / 2 + 1; x < tileSize; x++) caminoL.Add(new Vector2Int(x, tileSize / 2));
        formasDeTile.Add(new TileShape(
            "L",
            caminoL,
            new Vector2Int(tileSize / 2, 0),
            new List<Vector2Int> { new Vector2Int(tileSize - 1, tileSize / 2) },
            new List<(Vector2Int, Vector2Int)>()
        ));
        // Forma Cruz (centro, sale arriba, abajo, izq, der)
        var cruzCentro = new Vector2Int(tileSize / 2, tileSize / 2);
        var caminoCruz = new List<Vector2Int> { cruzCentro };
        for (int y = 0; y < tileSize; y++) if (y != tileSize / 2) caminoCruz.Add(new Vector2Int(tileSize / 2, y));
        for (int x = 0; x < tileSize; x++) if (x != tileSize / 2) caminoCruz.Add(new Vector2Int(x, tileSize / 2));

        // Conexiones: centro con cada rama
        var conexionesCruz = new List<(Vector2Int, Vector2Int)>();
        for (int y = 0; y < tileSize; y++)
            if (y != tileSize / 2) conexionesCruz.Add((cruzCentro, new Vector2Int(tileSize / 2, y)));
        for (int x = 0; x < tileSize; x++)
            if (x != tileSize / 2) conexionesCruz.Add((cruzCentro, new Vector2Int(x, tileSize / 2)));

        formasDeTile.Add(new TileShape(
            "Cruz",
            caminoCruz,
            cruzCentro, // entrada en el centro
            new List<Vector2Int> {
        new Vector2Int(tileSize / 2, 0),
        new Vector2Int(tileSize / 2, tileSize - 1),
        new Vector2Int(0, tileSize / 2),
        new Vector2Int(tileSize - 1, tileSize / 2)
            },
            conexionesCruz
        ));
    }


    void Start()
    {
        grafo = new GrafoMA();
        grafo.InicializarGrafo();
        // Primer tile (entrada = core, salida = centro última fila)
        CrearTileYCamino(0, null, formasDeTile[0]);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            AgregarTile();
            Debug.Log("Nuevo tile agregado.");
        }
    }

    void CrearTileYCamino(int tileIndex, int? lastExit, TileShape forma)
    {
        int tileOffset = tileIndex * tileSize * tileSize;

        // Info estructural para debug (opcional)
        var tileInfo = new TileParentInfo();
        tileInfo.tileIndex = tileIndex;

        // Guardá vértices y aristas de este tile
        foreach (var cell in forma.camino)
            tileInfo.vertices.Add(tileOffset + (cell.y * tileSize + cell.x));
        foreach (var con in forma.conexiones)
            tileInfo.aristas.Add((
                tileOffset + (con.from.y * tileSize + con.from.x),
                tileOffset + (con.to.y * tileSize + con.to.x),
                1));

        // 1. Agregar vértices al grafo
        foreach (var cell in forma.camino)
            grafo.AgregarVertice(tileOffset + (cell.y * tileSize + cell.x));
        // 2. Conexiones internas
        foreach (var con in forma.conexiones)
            grafo.AgregarArista(0,
                tileOffset + (con.from.y * tileSize + con.from.x),
                tileOffset + (con.to.y * tileSize + con.to.x), 1);

        // 3. Caminos adyacentes
        for (int i = 0; i < forma.camino.Count - 1; i++)
        {
            var a = forma.camino[i];
            var b = forma.camino[i + 1];
            grafo.AgregarArista(0,
                tileOffset + (a.y * tileSize + a.x),
                tileOffset + (b.y * tileSize + b.x), 1);
        }

        // 4. Entrada real: si hay lastExit, conectá con la entrada local
        int entrada = tileOffset + (forma.entrada.y * tileSize + forma.entrada.x);
        if (lastExit.HasValue)
            grafo.AgregarArista(0, lastExit.Value, entrada, 1);

        // 5. Generar el path de este tile
        List<int> path;
        if (forma.nombre == "Cruz")
        {
            int idxSalidaCruz = Random.Range(0, forma.salidas.Count);
            Vector2Int salidaLocalCruz = forma.salidas[idxSalidaCruz];
            int salida = tileOffset + (salidaLocalCruz.y * tileSize + salidaLocalCruz.x);
            path = ObtenerCaminoBFS(entrada, salida);
        }
        else
        {
            // Para las demás formas, conectá todas las salidas
            HashSet<int> fullPath = new HashSet<int>();
            foreach (var salidaVec in forma.salidas)
            {
                int salida = tileOffset + (salidaVec.y * tileSize + salidaVec.x);
                var subPath = ObtenerCaminoBFS(entrada, salida);
                if (subPath != null)
                    foreach (var idx in subPath)
                        fullPath.Add(idx);
            }
            path = new List<int>(fullPath);
        }
        tilePaths.Add(path);

        // --- Offset para instanciar correctamente ---
        Vector3 entradaLocalWorld = new Vector3(
            forma.entrada.x * cellSize, 0.1f, forma.entrada.y * cellSize
        );
        Vector3 tileVisualOffset;
        if (tileIndex == 0)
        {
            tileVisualOffset = Vector3.zero;
        }
        else
        {
            // Alinear la entrada de este tile con la última salida global del anterior
            tileVisualOffset = lastExitWorldPos - entradaLocalWorld;
        }

        Vector2Int anteriorSalida = lastExitLocal; // o guardá la última salida local/global
        Vector2Int nuevaEntrada = forma.entrada;

        // Diferencia entre las posiciones (puede ser +1 en x, +1 en y, etc.)
        Vector2Int diff = nuevaEntrada - anteriorSalida;

        // Determiná la rotación
        float rotationY = 0f;
        if (diff == Vector2Int.up) rotationY = 180f;
        else if (diff == Vector2Int.right) rotationY = -90f;
        else if (diff == Vector2Int.left) rotationY = 90f;
        // else (down): rotationY = 0 (por default)

        // --- Instanciá el tile agrupando los prefabs bajo un parent ---
        InstanciarTile(tileOffset, path, tileIndex == 0, tileIndex, forma, tileVisualOffset, tileInfo.vertices, tileInfo.aristas);


        // --- Guardá el offset de este tile (si necesitás una lista de offsets) ---
        if (tileOffsets.Count <= tileIndex)
            tileOffsets.Add(tileVisualOffset);
        else
            tileOffsets[tileIndex] = tileVisualOffset;

        // --- Guardar la salida real (mundial) para el próximo tile ---
        int idxSalida = Random.Range(0, forma.salidas.Count);
        Vector2Int salidaLocal = forma.salidas[idxSalida];
        Vector3 salidaWorld = new Vector3(
            salidaLocal.x * cellSize, 0.1f, salidaLocal.y * cellSize
        ) + tileVisualOffset;
        lastExitWorldPos = salidaWorld;
        lastExitLocal = salidaLocal;

        // --- Guardar la posición de entrada global ---
        if (tileIndex == 0)
            pathEntrancesWorld.Add(entradaLocalWorld);
        else
            pathEntrancesWorld.Add(tileVisualOffset + entradaLocalWorld);

        tileCount++;
    }

    List<int> ObtenerCaminoBFS(int start, int end)
    {
        Queue<int> queue = new Queue<int>();
        Dictionary<int, int> prev = new Dictionary<int, int>();
        queue.Enqueue(start);
        prev[start] = -1;

        while (queue.Count > 0)
        {
            int curr = queue.Dequeue();
            if (curr == end)
            {
                // reconstruir el camino
                List<int> path = new List<int>();
                int node = end;
                while (node != -1)
                {
                    path.Add(node);
                    node = prev[node];
                }
                path.Reverse();
                return path;
            }
            // vecinos (busca todos los que están conectados por arista)
            for (int i = 0; i < grafo.cantNodos; i++)
            {
                if (grafo.MAdy[grafo.Vert2Indice(curr), i] != 0)
                {
                    int vecino = grafo.Etiqs[i];
                    if (!prev.ContainsKey(vecino))
                    {
                        queue.Enqueue(vecino);
                        prev[vecino] = curr;
                    }
                }
            }
        }
        return null; // no hay camino
    }

    void InstanciarTile(int offset, List<int> path, bool isFirstTile, int tileIndex, TileShape forma, Vector3 tileVisualOffset, List<int> vertices, List<(int, int, int)> aristas)
    {
        GameObject tileParent = new GameObject($"Tile_{tileIndex}_Parent");
        tileParent.transform.parent = this.transform;
        tileParent.transform.position = tileVisualOffset;
        tileParent.transform.rotation = Quaternion.Euler(0, 0, 0); // agregar rotationY

        HashSet<Vector2Int> posicionesCamino = new HashSet<Vector2Int>();
        if (isFirstTile && path.Count > 0)
        {
            int idxCore = path[0] - offset;
            int px = idxCore % tileSize;
            int py = idxCore / tileSize;
            Vector3 corePos = new Vector3(px * cellSize, 0.2f, py * cellSize) + tileVisualOffset;
            Instantiate(corePrefab, corePos, Quaternion.identity, tileParent.transform);
        }
        foreach (int idx in path)
        {
            int local = idx - offset;
            int px = local % tileSize;
            int py = local / tileSize;
            posicionesCamino.Add(new Vector2Int(px, py));
            Vector3 pos = new Vector3(px * cellSize, 0.1f, py * cellSize) + tileVisualOffset;
            Instantiate(pathPrefab, pos, Quaternion.identity, tileParent.transform);
        }
        for (int y = 0; y < tileSize; y++)
            for (int x = 0; x < tileSize; x++)
                if (!posicionesCamino.Contains(new Vector2Int(x, y)))
                {
                    Vector3 pos = new Vector3(x * cellSize, 0f, y * cellSize) + tileVisualOffset;
                    Instantiate(cellPrefab, pos, Quaternion.identity, tileParent.transform);
                }

        // Extra: agregá el script con info
        TileParentInfo info = tileParent.AddComponent<TileParentInfo>();
        info.tileIndex = tileIndex;
        info.vertices = new List<int>(vertices);
        info.aristas = new List<(int, int, int)>(aristas);
    }




    public void AgregarTile()
    {
        // Aleatorio:
        var forma = formasDeTile[Random.Range(0, formasDeTile.Count)];
        int? lastExit = tilePaths.Count > 0 && tilePaths[^1].Count > 0 ? tilePaths[^1][^1] : (int?)null;
        CrearTileYCamino(tileCount, lastExit, forma);
    }

    public int GetTotalPaths()
    {
        return tilePaths.Count;
    }

    public Vector3 GetPathEntranceWorld(int pathIndex)
    {
        if (pathEntrancesWorld.Count > pathIndex)
            return pathEntrancesWorld[pathIndex];
        else
            return Vector3.zero;
    }

    public Vector3[] GetPathPositions(int tileIndex = 0)
    {
        if (tilePaths.Count > tileIndex)
        {
            List<int> indices = tilePaths[tileIndex];
            List<Vector3> positions = new List<Vector3>();
            foreach (int idx in indices)
            {
                int tileOffset = tileIndex * tileSize * tileSize;
                int local = idx - tileOffset;
                int x = local % tileSize;
                int y = local / tileSize;
                Vector3 pos = new Vector3(x * cellSize + tileOffset % 100, 0.1f, y * cellSize + tileOffset / 100 * tileSize * cellSize);
                positions.Add(pos);
            }
            return positions.ToArray();
        }
        else
        {
            Debug.LogWarning("No existe ese tile.");
            return new Vector3[0];
        }
    }
    public Vector3[] GetFullPathPositionsForward()
    {
        List<Vector3> globalPath = new List<Vector3>();

        // Recorre todos los tiles en orden de generación
        for (int tileIndex = 0; tileIndex < tilePaths.Count; tileIndex++)
        {
            List<int> indices = tilePaths[tileIndex];
            Vector3 tileOffset = tileOffsets[tileIndex]; // Usa el offset guardado de ese tile

            foreach (int idx in indices)
            {
                int local = idx - tileIndex * tileSize * tileSize;
                int x = local % tileSize;
                int y = local / tileSize;
                Vector3 pos = new Vector3(x * cellSize, 0.1f, y * cellSize) + tileOffset;
                globalPath.Add(pos);
            }
        }
        return globalPath.ToArray();
    }



    // Opcional: para obtener la posición de entrada del último tile (donde debe spawnear el enemigo)
    public Vector3 GetLastTileEntryWorldPos()
    {
        if (pathEntrancesWorld.Count > 0)
            return pathEntrancesWorld[pathEntrancesWorld.Count - 1];
        else
            return Vector3.zero;
    }



    [System.Serializable]
    public class TileShape
    {
        public string nombre;
        public List<Vector2Int> camino; // celdas del tile que son camino
        public Vector2Int entrada;      // celda de entrada relativa
        public List<Vector2Int> salidas;// celdas de salida relativas (puede haber varias)
        public List<(Vector2Int from, Vector2Int to)> conexiones; // conexiones internas

        public TileShape(string nombre, List<Vector2Int> camino, Vector2Int entrada, List<Vector2Int> salidas, List<(Vector2Int, Vector2Int)> conexiones)
        {
            this.nombre = nombre;
            this.camino = camino;
            this.entrada = entrada;
            this.salidas = salidas;
            this.conexiones = conexiones;
        }
    }
}
