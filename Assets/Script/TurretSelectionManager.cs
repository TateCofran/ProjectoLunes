using UnityEngine;

public class TurretSelectionManager : MonoBehaviour
{
    public static TurretSelectionManager Instance;

    public TurretSelection heavyTurret;
    public TurretSelection fastTurret;
    public TurretSelection goldTurret;
    public TurretSelection AOETurret;
    public TurretSelection SlowTurret;


    [HideInInspector] public TurretSelection selectedTurret;

    void Awake()
    {
        Instance = this;
    }

    public void SelectHeavyTurret()
    {
        selectedTurret = heavyTurret;
        Debug.Log("Torreta pesada seleccionada.");
    }

    public void SelectFastTurret()
    {
        selectedTurret = fastTurret;
        Debug.Log("Torreta rápida seleccionada.");
    }

    public void SelectGoldTurret()
    {
        selectedTurret = goldTurret;
        Debug.Log("Torreta de oro seleccionada.");
    }
    public void SelectAOETurret()
    {
        selectedTurret = AOETurret;
        Debug.Log("Torreta de AOE seleccionada.");
    }
    public void SelectSlowTurret()
    {
        selectedTurret = SlowTurret;
        Debug.Log("Torreta de Slow seleccionada.");
    }
}

[System.Serializable]
public class TurretSelection
{
    public string turretId;            // ID usado para buscar en el JSON
    public GameObject turretPrefab;   // Prefab base de la torreta
}
