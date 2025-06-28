using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TurretInfoUI : MonoBehaviour
{
    public static TurretInfoUI Instance;
    public GameObject panelUI; // referencia solo al panel que querés ocultar

    [Header("UI References")]
    public TMP_Text turretNameText;
    public TMP_Text damageText;
    public TMP_Text rangeText;
    public TMP_Text fireRateText;
    public TMP_Text totalDamageText;

    public Button sellButton;
    public Button upgradeButton;
    public TMP_Text sellButtonText;
    public TMP_Text upgradeButtonText;

    public Button changeTargetModeButton;
    public TMP_Text changeTargetModeButtonText;

    private Turret currentTurret;

    private int upgradeCost = 15;
    private float upgradeMultiplier = 1.3f;
    private float statMultiplier = 1.25f;
    private float refundPercentage = 0.6f;

    void Awake()
    {
        Instance = this;

        sellButton.onClick.AddListener(SellTurret);
        upgradeButton.onClick.AddListener(UpgradeTurret);

        Hide();
    }

    public void Initialize(Turret turret)
    {
        currentTurret = turret;
        upgradeCost = 15;
        UpdateInfo();

        changeTargetModeButton.onClick.RemoveAllListeners();
        changeTargetModeButton.onClick.AddListener(ChangeTargetingMode);

        UpdateTargetModeText(currentTurret.currentTargetingMode);
    }


    public void UpdateInfo()
    {
        if (currentTurret == null) return;

        turretNameText.text = $"{currentTurret.turretName} (Nivel {currentTurret.upgradeLevel})";

        if (currentTurret.turretData != null && currentTurret.turretData.type == "support")
        {
            // Ocultar stats de ataque y targeting
            damageText.text = "Genera Oro por Oleada";
            rangeText.text = "-";
            fireRateText.text = "-";
            totalDamageText.text = "-";

            // Ocultar botones innecesarios
            upgradeButton.gameObject.SetActive(false);
            sellButton.gameObject.SetActive(true); // Podés venderla igual
            changeTargetModeButton.gameObject.SetActive(false);
        }
        else
        {
            // Mostrar datos normales
            damageText.text = $"Daño: {currentTurret.damage}";
            rangeText.text = $"Rango: {currentTurret.range}";
            fireRateText.text = $"Velocidad: {currentTurret.fireRate}";
            totalDamageText.text = $"Daño total: {currentTurret.totalDamageDealt}";

            upgradeButton.gameObject.SetActive(true);
            sellButton.gameObject.SetActive(true);
            changeTargetModeButton.gameObject.SetActive(true);
        }

        sellButtonText.text = $"Vender ({GetSellValue()} Oro)";
        upgradeButtonText.text = currentTurret.upgradeLevel < currentTurret.maxUpgradeLevel
            ? $"Mejorar ({upgradeCost} Oro)"
            : "Nivel Máximo";
    }

    public void Hide()
    {
        panelUI.SetActive(false); // Oculta solo el panel UI
    }

    public void Show()
    {
        panelUI.SetActive(true);  // Muestra solo el panel UI
    }

    int GetSellValue()
    {
        // Calcula valor de venta total, incluyendo costo base y mejoras
        int totalInvested = currentTurret.cost + Mathf.RoundToInt((upgradeCost / upgradeMultiplier - 15));
        return Mathf.RoundToInt(totalInvested * refundPercentage);
    }

    public void SellTurret()
    {
        int refund = GetSellValue();
        GoldManager.Instance.AddGold(refund);

        Destroy(currentTurret.gameObject);
        Hide();

        Debug.Log($"Torreta vendida. Reintegrado: {refund} Oro.");
    }

    public void UpgradeTurret()
    {
        if (currentTurret.upgradeLevel >= currentTurret.maxUpgradeLevel)
        {
            Debug.Log("Nivel máximo alcanzado.");
            return;
        }

        if (GoldManager.Instance.HasEnoughGold(upgradeCost))
        {
            GoldManager.Instance.SpendGold(upgradeCost);

            currentTurret.damage = Mathf.Round(currentTurret.damage * statMultiplier);
            currentTurret.range = Mathf.Round(currentTurret.range * statMultiplier);
            currentTurret.fireRate = Mathf.Round(currentTurret.fireRate * statMultiplier);

            currentTurret.upgradeLevel++;
            upgradeCost = Mathf.RoundToInt(upgradeCost * upgradeMultiplier);

            currentTurret.UpdateRangeCircle(); // <- Llamada importante

            UpdateInfo();

            Debug.Log($"Torreta mejorada a nivel {currentTurret.upgradeLevel}. Nuevo costo de mejora: {upgradeCost} Oro.");
        }
        else
        {
            Debug.Log("No hay suficiente oro para mejorar.");
        }
    }

    public void UpdateTargetModeText(Turret.TargetingMode mode)
    {
        switch (mode)
        {
            case Turret.TargetingMode.Closest:
                changeTargetModeButtonText.text = "Ataca al más Cercano";
                break;
            case Turret.TargetingMode.Farthest:
                changeTargetModeButtonText.text = "Ataca al más Lejano";
                break;
            case Turret.TargetingMode.HighestHealth:
                changeTargetModeButtonText.text = "Ataca al de más Vida";
                break;
            case Turret.TargetingMode.LowestHealth:
                changeTargetModeButtonText.text = "Ataca al de menos Vida";
                break;
        }
    }

    void ChangeTargetingMode()
    {
        currentTurret.ChangeTargetingMode();
    }
}
