using UnityEngine;

public class CellInteraction : MonoBehaviour
{
    private bool hasTurret = false;
    private Renderer cellRenderer;
    private Color originalColor;

    private float lastClickTime = 0f;
    private float doubleClickThreshold = 0.3f;

    void Start()
    {
        cellRenderer = GetComponent<Renderer>();

        if (cellRenderer != null)
        {
            originalColor = cellRenderer.material.color;
        }
        else
        {
            Debug.LogError("CellInteraction: No se encontró el Renderer en " + gameObject.name);
        }
    }
    void OnMouseEnter()
    {
        if (cellRenderer == null)
            return;

        // Verificar si hay una torreta seleccionada
        var selection = TurretSelectionManager.Instance?.selectedTurret;
        if (selection == null || hasTurret || CompareTag("Path") || CompareTag("Core"))
        {
            cellRenderer.material.color = Color.red;
            return;
        }

        // Verificar que la base de datos esté disponible
        var data = TurretDatabase.Instance?.GetTurretData(selection.turretId);
        if (data != null && GoldManager.Instance.HasEnoughGold(data.cost))
            cellRenderer.material.color = Color.green;
        else
            cellRenderer.material.color = Color.red;
    }

    void OnMouseExit()
    {
        if (cellRenderer != null)
            cellRenderer.material.color = originalColor;
    }

    void OnMouseDown()
    {
        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            if (!hasTurret && CanPlaceTurret() && TurretPlacementManager.Instance.CanPlaceTurret())
            {
                var selection = TurretSelectionManager.Instance.selectedTurret;
                if (selection == null)
                {
                    Debug.Log("No hay torreta seleccionada.");
                    return;
                }

                var turretData = TurretDatabase.Instance.GetTurretData(selection.turretId);
                if (turretData == null)
                {
                    Debug.LogError($"No se encontró la torreta con ID: {selection.turretId}");
                    return;
                }

                if (!GoldManager.Instance.HasEnoughGold(turretData.cost))
                {
                    Debug.Log("No tenés suficiente oro para colocar esta torreta.");
                    return;
                }

                GoldManager.Instance.SpendGold(turretData.cost);

                GameObject newTurretGO = Instantiate(selection.turretPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                hasTurret = true;

                Turret newTurret = newTurretGO.GetComponent<Turret>();
                newTurret.ApplyData(turretData); // <- Cargamos stats desde JSON
                TurretManager.Instance.RegisterTurret(newTurret);

                TurretPlacementManager.Instance.RegisterPlacement(); // aplica delay

                Debug.Log($"Torreta '{turretData.name}' colocada. Costó {turretData.cost} de oro.");
            }
        }

        lastClickTime = Time.time;
    }

    bool CanPlaceTurret()
    {
        return !hasTurret && !CompareTag("Path") && !CompareTag("Core");
    }
}
