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
    private List<List<int>> tilePaths = new List<List<int>>();
    private int tileCount = 0;

    private Vector3 lastExitWorldPos = Vector3.zero;
    private Vector2Int lastExitLocal = Vector2Int.zero;

    public List<TileShape> formasDeTile = new List<TileShape>();

    private List<Vector3> pathEntrancesWorld = new List<Vector3>();
    private List<Vector3> tileOffsets = new List<Vector3>();
    private Vector3 currentGlobalOffset = Vector3.zero;
    private float currentRotationY = 0f; // En GridManager
    private List<TileTransform> tileTransforms = new List<TileTransform>();


    void Awake()
    {
        // Recto vertical
        var caminoRecto = new List<Vector2Int>();
        for (int y = 0; y < tileSize; y++) caminoRecto.Add(new Vector2Int(tileSize / 2, y));
        formasDeTile.Add(new TileShape(
            "Recto",
            caminoRecto,
            new Vector2Int(tileSize / 2, 0),
            new List<Vector2Int> { new Vector2Int(tileSize / 2, tileSize - 1) },
            new List<(Vector2Int, Vector2Int)>()
        ));

        // L
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
        //cruz
        var cruzCentro = new Vector2Int(tileSize / 2, tileSize / 2);

        // Camino: fila y columna central
        var caminoCruz = new List<Vector2Int>();
        for (int i = 0; i < tileSize; i++)
        {
            caminoCruz.Add(new Vector2Int(tileSize / 2, i)); // columna central (vertical)
            caminoCruz.Add(new Vector2Int(i, tileSize / 2)); // fila central (horizontal)
        }
        // Eliminar duplicados del centro
        caminoCruz = caminoCruz.Distinct().ToList();

        // Conexiones: (opcional, solo si tu grafo requiere)
        // Por ejemplo: conectá el centro a todos los extremos de la cruz si hacés BFS

        // Salidas posibles (los 4 extremos)
        var salidasCruz = new List<Vector2Int>
{
    new Vector2Int(tileSize / 2, 0),                 // arriba
    new Vector2Int(tileSize / 2, tileSize - 1),      // abajo
    new Vector2Int(0, tileSize / 2),                 // izquierda
    new Vector2Int(tileSize - 1, tileSize / 2)       // derecha
};

        // Entrada es el centro
        var entradaCruz = cruzCentro;

        // Agregá la forma:
        formasDeTile.Add(new TileShape(
            "Cruz",
            caminoCruz,
            entradaCruz,
            salidasCruz,
            new List<(Vector2Int, Vector2Int)>() // o conexiones según tu lógica
        ));

    }

    void Start()
    {
        grafo = new GrafoMA();
        grafo.InicializarGrafo();
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

    public void AgregarTile()
    {
        var forma = formasDeTile[Random.Range(0, formasDeTile.Count)];
        int? lastExit = tilePaths.Count > 0 && tilePaths[^1].Count > 0 ? tilePaths[^1][^1] : (int?)null;
        CrearTileYCamino(tileCount, lastExit, forma);
    }

    void CrearTileYCamino(int tileIndex, int? lastExit, TileShape forma)
    {
        int tileOffset = tileIndex * tileSize * tileSize;

        // ---------------- PATH LOGIC (NO TOCAR)
        List<int> path;
        int entrada = tileOffset + (forma.entrada.y * tileSize + forma.entrada.x);

        if (forma.nombre == "Cruz")
        {
            int idxSalidaCruz = Random.Range(0, forma.salidas.Count);
            Vector2Int salidaLocalCruz = forma.salidas[idxSalidaCruz];
            int salida = tileOffset + (salidaLocalCruz.y * tileSize + salidaLocalCruz.x);
            path = ObtenerCaminoBFS(entrada, salida);
        }
        else
        {
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

        // -------------------- CALCULAR POSICIÓN Y ROTACIÓN DEL TILE
        Vector3 entradaLocalWorld = new Vector3(forma.entrada.x * cellSize, 0, forma.entrada.y * cellSize);
        Vector3 salidaLocalWorld = new Vector3(forma.salidas[0].x * cellSize, 0, forma.salidas[0].y * cellSize);

        Vector3 position;
        Quaternion rotation;

        if (tileIndex == 0)
        {
            // Primer tile, sin rotación ni offset
            position = Vector3.zero;
            rotation = Quaternion.identity;
            lastExitWorldPos = salidaLocalWorld;
            lastExitLocal = forma.salidas[0];
        }
        else
        {
            // Anterior tile
            TileTransform lastTile = tileTransforms[tileIndex - 1];

            // Direcciones
            Vector3 dirEntradaLocal = (salidaLocalWorld - entradaLocalWorld).normalized;   // En tile local
            Vector3 dirGlobal = (lastExitWorldPos - lastTile.position).normalized;    // En mundo

            // Ángulo entre entrada local y dirección deseada global
            float angle = Vector3.SignedAngle(dirEntradaLocal, dirGlobal, Vector3.up);
            rotation = Quaternion.Euler(0, angle, 0) * lastTile.rotation;

            // Posición para que la entrada quede justo en la última salida global
            position = lastExitWorldPos - rotation * entradaLocalWorld;

            lastExitWorldPos = position + rotation * salidaLocalWorld;
            lastExitLocal = forma.salidas[0];
        }
        tileTransforms.Add(new TileTransform(position, rotation));

        // ------------------- INSTANCIAR TILE PARENT Y SUS PREFABS HIJOS (SOLO POSICIÓN LOCAL)
        GameObject tileParent = new GameObject($"Tile_{tileIndex}_Parent");
        tileParent.transform.parent = this.transform;
        tileParent.transform.position = position;
        tileParent.transform.rotation = rotation;

        // Instanciar caminos (SOLO POSICIÓN LOCAL)
        HashSet<Vector2Int> posicionesCamino = new HashSet<Vector2Int>();
        foreach (int idx in path)
        {
            int local = idx - tileOffset;
            int px = local % tileSize;
            int py = local / tileSize;
            posicionesCamino.Add(new Vector2Int(px, py));
            Vector3 pos = new Vector3(px * cellSize, 0.1f, py * cellSize);
            Instantiate(pathPrefab, pos, Quaternion.identity, tileParent.transform);
        }
        // Instanciar celdas normales
        for (int y = 0; y < tileSize; y++)
            for (int x = 0; x < tileSize; x++)
                if (!posicionesCamino.Contains(new Vector2Int(x, y)))
                {
                    Vector3 pos = new Vector3(x * cellSize, 0f, y * cellSize);
                    Instantiate(cellPrefab, pos, Quaternion.identity, tileParent.transform);
                }
        // Core solo en el primero
        if (tileIndex == 0 && path.Count > 0)
        {
            int idxCore = path[0] - tileOffset;
            int px = idxCore % tileSize;
            int py = idxCore / tileSize;
            Vector3 corePos = new Vector3(px * cellSize, 0.2f, py * cellSize);
            Instantiate(corePrefab, corePos, Quaternion.identity, tileParent.transform);
        }

        tileCount++;
    }

    void InstanciarTile(
        int offset, List<int> path, bool isFirstTile, int tileIndex, TileShape forma,
        Vector3 tileVisualOffset, float rotationY, List<int> vertices, List<(int, int, int)> aristas)
    {
        GameObject tileParent = new GameObject($"Tile_{tileIndex}_Parent");
        tileParent.transform.parent = this.transform;
        tileParent.transform.position = tileVisualOffset;
        tileParent.transform.rotation = Quaternion.Euler(0, rotationY, 0);

        HashSet<Vector2Int> posicionesCamino = new HashSet<Vector2Int>();
        // Core
        if (isFirstTile && path.Count > 0)
        {
            int idxCore = path[0] - offset;
            int px = idxCore % tileSize;
            int py = idxCore / tileSize;
            Vector3 corePos = new Vector3(px * cellSize, 0.2f, py * cellSize); // SIN tileVisualOffset
            Instantiate(corePrefab, corePos, Quaternion.identity, tileParent.transform);
        }
        // Caminos
        foreach (int idx in path)
        {
            int local = idx - offset;
            int px = local % tileSize;
            int py = local / tileSize;
            posicionesCamino.Add(new Vector2Int(px, py));
            Vector3 pos = new Vector3(px * cellSize, 0.1f, py * cellSize); // SIN tileVisualOffset
            Instantiate(pathPrefab, pos, Quaternion.identity, tileParent.transform);
        }
        // Celdas normales
        for (int y = 0; y < tileSize; y++)
            for (int x = 0; x < tileSize; x++)
                if (!posicionesCamino.Contains(new Vector2Int(x, y)))
                {
                    Vector3 pos = new Vector3(x * cellSize, 0f, y * cellSize); // SIN tileVisualOffset
                    Instantiate(cellPrefab, pos, Quaternion.identity, tileParent.transform);
                }

        TileParentInfo info = tileParent.AddComponent<TileParentInfo>();
        info.tileIndex = tileIndex;
        info.vertices = new List<int>(vertices);
        info.aristas = new List<(int, int, int)>(aristas);
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
        return null;
    }

    // ----------- GETTERS para WaveSpawner/enemigos -----------

    public int GetTotalPaths() => tilePaths.Count;

    public Vector3[] GetPathPositions(int tileIndex = 0)
    {
        if (tilePaths.Count > tileIndex)
        {
            List<int> indices = tilePaths[tileIndex];
            List<Vector3> positions = new List<Vector3>();
            foreach (int idx in indices)
            {
                int tOffset = tileIndex * tileSize * tileSize;
                int local = idx - tOffset;
                int x = local % tileSize;
                int y = local / tileSize;
                Vector3 pos = new Vector3(x * cellSize, 0.1f, y * cellSize) + tileOffsets[tileIndex];
                positions.Add(pos);
            }
            return positions.ToArray();
        }
        return new Vector3[0];
    }

    // Devuelve TODO el path expandido (entrada tile0, ... salida tileN)
    public Vector3[] GetFullPathPositionsForward()
    {
        List<Vector3> positions = new List<Vector3>();
        for (int t = 0; t < tilePaths.Count; t++)
        {
            var indices = tilePaths[t];
            foreach (int idx in indices)
            {
                int tOffset = t * tileSize * tileSize;
                int local = idx - tOffset;
                int x = local % tileSize;
                int y = local / tileSize;
                Vector3 pos = new Vector3(x * cellSize, 0.1f, y * cellSize) + tileOffsets[t];
                positions.Add(pos);
            }
        }
        return positions.ToArray();
    }

    public Vector3 GetLastTileEntryWorldPos()
    {
        return pathEntrancesWorld.Count > 0 ? pathEntrancesWorld[^1] : Vector3.zero;
    }
    Vector3 RotatePoint(Vector3 point, float angleY, Vector3 pivot = default)
    {
        Quaternion rot = Quaternion.Euler(0, angleY, 0);
        return rot * (point - pivot) + pivot;
    }
    public struct TileTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public TileTransform(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }


    // --- Estructuras ---

    [System.Serializable]
    public class TileShape
    {
        public string nombre;
        public List<Vector2Int> camino;
        public Vector2Int entrada;
        public List<Vector2Int> salidas;
        public List<(Vector2Int from, Vector2Int to)> conexiones;

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

