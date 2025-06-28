using UnityEngine;

public class TurretPlacementManager : MonoBehaviour
{
    public static TurretPlacementManager Instance;

    public float placementDelay = 1.0f;
    private float nextAllowedPlacementTime = 0f;

    void Awake()
    {
        Instance = this;
    }

    public bool CanPlaceTurret()
    {
        return Time.time >= nextAllowedPlacementTime &&
               TurretSelectionManager.Instance.selectedTurret != null;
    }


    public void RegisterPlacement()
    {
        nextAllowedPlacementTime = Time.time + placementDelay;
    }
    public bool TryPlaceTurret()
    {
        if (!CanPlaceTurret())
        {
            Debug.Log("No se puede colocar torreta todavía.");
            return false;
        }

        RegisterPlacement();
        return true;
    }

}
