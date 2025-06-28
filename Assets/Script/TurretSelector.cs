using UnityEngine;

public class TurretSelector : MonoBehaviour
{
    public static TurretSelector Instance;
    private Turret selectedTurret;

    void Awake()
    {
        Instance = this;
    }

    public void SelectTurret(Turret turret)
    {
        // Si es la misma torreta, deseleccionar
        if (selectedTurret == turret)
        {
            selectedTurret.HideRangeLine();
            TurretInfoUI.Instance.Hide();
            selectedTurret = null;
            return;
        }

        // Ocultar el rango y UI de la anterior
        if (selectedTurret != null)
        {
            selectedTurret.HideRangeLine();
            TurretInfoUI.Instance.Hide();
        }

        selectedTurret = turret;
        selectedTurret.ShowRangeLine();

        // Mostrar la UI actualizada
        TurretInfoUI.Instance.Initialize(turret);
        TurretInfoUI.Instance.Show();
    }




    public bool IsSelected(Turret turret)
    {
        return selectedTurret == turret;
    }

    public void Deselect()
    {
        if (selectedTurret != null)
        {
            selectedTurret.HideRangeLine();
            TurretInfoUI.Instance.Hide();
            selectedTurret = null;
        }
    }
}
