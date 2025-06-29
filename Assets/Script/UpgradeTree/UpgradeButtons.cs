using UnityEngine;
using UnityEngine.UI;


public class MejorasBotonHandler : MonoBehaviour
{
    public string torretaID; // "Normal" o "Gold"
    public string mejoraID;  // "damage", "range", etc.
    public Button miBoton;

    public void DesbloquearMejora()
    {
        if(UpgradeManager.Instance == null) { Debug.LogError("UpgradeManager.Instance es null"); return; }
        if (string.IsNullOrEmpty(torretaID)) { Debug.LogError("torretaID es null o vacío"); return; }
        if (string.IsNullOrEmpty(mejoraID)) { Debug.LogError("mejoraID es null o vacío"); return; }
        if (miBoton == null) { Debug.LogError("miBoton es null"); return; }

        UpgradeManager.Instance.DesbloquearMejora(torretaID, mejoraID);
        Debug.Log("Desbloqueada: " + torretaID + " " + mejoraID);
        miBoton.interactable = false;
    }
}
