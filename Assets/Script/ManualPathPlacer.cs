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
    public int extensionLength = 4; 
    public bool enableAutoBuilding = true;

    [Header("Integration")]
    public bool integrateWithGridManager = true;

    private GridManager gridManager;
    private WaveSpawner waveSpawner;
    private Dictionary<KeyCode, (Vector2Int direction, string name)> simpleDirections;

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>();
        waveSpawner = FindObjectOfType<WaveSpawner>();
        
        SetupSimpleDirections();
        
    }

    void SetupSimpleDirections()
    {
        
        simpleDirections = new Dictionary<KeyCode, (Vector2Int, string)>
        {
            
            { KeyCode.Alpha1, (Vector2Int.up, "ARRIBA") },       
            { KeyCode.Alpha2, (Vector2Int.right, "DERECHA") },    
            { KeyCode.Alpha3, (Vector2Int.left, "IZQUIERDA") },  
            { KeyCode.Alpha4, (Vector2Int.down, "ABAJO") },       
            
          
            { KeyCode.UpArrow, (Vector2Int.up, "ARRIBA") },
            { KeyCode.RightArrow, (Vector2Int.right, "DERECHA") },
            { KeyCode.LeftArrow, (Vector2Int.left, "IZQUIERDA") },
            { KeyCode.DownArrow, (Vector2Int.down, "ABAJO") }
        };
    }

    void Update()
    {
        if (!enableAutoBuilding) return;
        HandleHotkeyInput();
    }

    void HandleHotkeyInput()
    {
        foreach (var kvp in simpleDirections)
        {
            if (Input.GetKeyDown(kvp.Key))
            {
                BuildPathInDirection(kvp.Value.direction, kvp.Value.name);
            }
        }
    }

    void BuildPathInDirection(Vector2Int direction, string directionName)
    {
        if (gridManager == null)
        {
            return;
        }
        


        var pathTile = CreateSimplePathTile(direction, directionName);
        
        if (pathTile != null)
        {
  
            ApplyPathExtension(pathTile, direction, directionName);
            
      
            
   
            UpdateActiveEnemies();
        }
      
    }

    GridManager.TileExpansion CreateSimplePathTile(Vector2Int direction, string directionName)
    {
      
        Vector2Int[] pathOffsets = new Vector2Int[extensionLength];
        
        for (int i = 0; i < extensionLength; i++)
        {
            pathOffsets[i] = direction * i;
        }

        return new GridManager.TileExpansion($"AutoPath-{directionName}", pathOffsets);
    }

    
    //Extensiones del path
    void ApplyPathExtension(GridManager.TileExpansion tile, Vector2Int direction, string directionName)
    {
        gridManager.tileCount++;
        GameObject tileParent = new GameObject($"AutoPath-{gridManager.tileCount}-{directionName}");
        tileParent.transform.parent = gridManager.transform;

    
        Vector2Int startingPoint = gridManager.currentPathEnd;
        Vector2Int nextPoint = startingPoint + direction;




        Vector2Int tileOffset = CalculateTileOffset(direction);
        Vector2Int bottomLeft = startingPoint + tileOffset;

 
        CreateTileCells(bottomLeft, tile.tileSize, tileParent);


        CreatePathSegments(tile, startingPoint, direction, tileParent);

      
    }

    Vector2Int CalculateTileOffset(Vector2Int direction)
    {
      
        if (direction == Vector2Int.up) 
            return new Vector2Int(-2, 1);
        else if (direction == Vector2Int.right) 
            return new Vector2Int(1, -2);
        else if (direction == Vector2Int.left) 
            return new Vector2Int(-5, -2);
        else if (direction == Vector2Int.down) 
            return new Vector2Int(-2, -5);
        else 
            return Vector2Int.zero;
    }

    void CreateTileCells(Vector2Int bottomLeft, Vector2Int tileSize, GameObject parent)
    {
        
        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                Vector2Int cellPos = new Vector2Int(bottomLeft.x + x, bottomLeft.y + y);
                Vector3 worldPos = new Vector3(cellPos.x * gridManager.cellSize, 0, cellPos.y * gridManager.cellSize);

           
                if (!gridManager.gridCells.ContainsKey(cellPos))
                {
                    gridManager.gridCells[cellPos] = Instantiate(gridManager.cellPrefab, worldPos, Quaternion.identity, parent.transform);
                }
                
            
                gridManager.CrearCelda(cellPos);
            }
        }
    }

    void CreatePathSegments(GridManager.TileExpansion tile, Vector2Int startPoint, Vector2Int direction, GameObject parent)
    {
        Vector2Int previousPos = startPoint;

    
        for (int i = 1; i < tile.pathOffsets.Length; i++) 
        {
            Vector2Int pathPos = startPoint + direction * i;
            Vector3 worldPos = new Vector3(pathPos.x * gridManager.cellSize, 0, pathPos.y * gridManager.cellSize);


            if (gridManager.gridCells.ContainsKey(pathPos))
            {
                Destroy(gridManager.gridCells[pathPos]);
                gridManager.gridCells.Remove(pathPos);
            }

          
            gridManager.gridCells[pathPos] = Instantiate(gridManager.pathPrefab, worldPos, Quaternion.identity, parent.transform);
            gridManager.pathPositions.Add(worldPos);

            gridManager.CrearCelda(pathPos);
            gridManager.ConectarCeldas(previousPos, pathPos, 1);

            previousPos = pathPos;
            
         
        }


        gridManager.currentPathEnd = previousPos;

 
        Vector2Int bottomLeft = CalculateTileOffset(direction) + startPoint;
        gridManager.placedTiles.Add(new GridManager.PlacedTileData(tile.tileName, bottomLeft));
    }

    void UpdateActiveEnemies()
    {
        if (waveSpawner != null)
        {
            waveSpawner.SendMessage("UpdateEnemyPaths", SendMessageOptions.DontRequireReceiver);
         
        }
    }

    void ForceCircularTile()
    {
        if (gridManager == null) return;


        
      
        int fakeWaveNumber = 2; 
        gridManager.ApplyRandomValidTileExpansion(fakeWaveNumber);
        
        
        UpdateActiveEnemies();
    }

    void GenerateMultipleExtensions()
    {
        if (gridManager == null) return;

      
        StartCoroutine(AutoBuildSequence());
    }

    System.Collections.IEnumerator AutoBuildSequence()
    {
       
        Vector2Int[] sequence = { Vector2Int.up, Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.up };
        string[] names = { "ARRIBA", "DERECHA", "ARRIBA", "IZQUIERDA", "ARRIBA" };

        for (int i = 0; i < sequence.Length; i++)
        {
            BuildPathInDirection(sequence[i], names[i]);
            yield return new WaitForSeconds(0.8f);
        }

    
    }

  

    void ClearExtensions()
    {
        if (gridManager == null) return;

  

 
        for (int i = gridManager.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = gridManager.transform.GetChild(i);
            if (child.name.Contains("AutoPath") || child.name.Contains("CustomTile"))
            {
             
                DestroyImmediate(child.gameObject);
            }
        }

   
    }

    void ToggleAutoBuilding()
    {
        enableAutoBuilding = !enableAutoBuilding;
        string status = enableAutoBuilding ? " ACTIVADO" : " DESACTIVADO";
      
    }



    bool CanBuildInDirection(Vector2Int direction)
    {
        if (gridManager == null) return false;
        
        Vector2Int targetPos = gridManager.currentPathEnd + direction;
        
   
        
        return true;
    }

    
    void OnDrawGizmos()
    {
        if (gridManager == null || !enableAutoBuilding) return;

      
        Vector3 currentWorldPos = new Vector3(
            gridManager.currentPathEnd.x * gridManager.cellSize, 
            0.5f, 
            gridManager.currentPathEnd.y * gridManager.cellSize
        );
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(currentWorldPos, 0.5f);
        
    
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.left, Vector2Int.down };
        Color[] colors = { Color.red, Color.blue, Color.yellow, Color.magenta };
        
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 directionWorldPos = currentWorldPos + new Vector3(
                directions[i].x * gridManager.cellSize, 
                0, 
                directions[i].y * gridManager.cellSize
            );
            
            Gizmos.color = colors[i];
            Gizmos.DrawLine(currentWorldPos, directionWorldPos);
            Gizmos.DrawWireCube(directionWorldPos, Vector3.one * 0.3f);
        }
    }
}