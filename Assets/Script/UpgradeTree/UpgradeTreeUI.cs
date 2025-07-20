using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeTreeUI : MonoBehaviour
{
    [Header("Upgrade Buttons & Texts")]
    public Button[] upgradeButtons;          
    public TMP_Text[] upgradeNameTexts;
    public TMP_Text[] upgradeDescCostTexts;
    public TMP_Text[] upgradeReqTexts;

    private UpgradeTree tree;
    private int[] upgradeIds = { 10, 5, 15, 12 }; 

    [Header("Gems")]
    public TMP_Text totalGemsText;

    void Start()
    {
        tree = new UpgradeTree();
        UpdateGemUI();

        for (int i = 0; i < upgradeIds.Length; i++)
        {
            var node = tree.Search(upgradeIds[i]);
            if (node == null) continue;

            upgradeNameTexts[i].text = node.Name;
            upgradeDescCostTexts[i].text = node.Description + "\nCosto: " + node.Cost;

            node.Unlocked = PlayerPrefs.GetInt("Upgrade_" + node.Id, 0) == 1;

            int idx = i; 
            upgradeButtons[i].onClick.AddListener(() => TryUnlock(node.Id));
        }

        ApplyAllUpgradeEffects();
        UpdateUI();
    }

    void TryUnlock(int id)
    {
        var node = tree.Search(id);
        if (node == null || node.Unlocked)
            return;

        string requisito = tree.GetMissingRequirement(id);
        if (!string.IsNullOrEmpty(requisito))
        {
            Debug.Log("Te falta: " + requisito);
            return;
        }

        var parent = tree.GetParent(id);
        bool puedeDesbloquear = node == tree.Root || (parent != null && parent.Unlocked);

        if (!puedeDesbloquear)
        {
            Debug.Log("No se cumplen los requisitos para desbloquear.");
            return;
        }

        if (!GemManager.Instance.SpendGemsPermanent(node.Cost))
        {
            Debug.Log("No tenés suficientes gemas permanentes.");
            return;
        }

        tree.Unlock(id);
        PlayerPrefs.SetInt("Upgrade_" + node.Id, 1);
        PlayerPrefs.Save();

        ApplyAllUpgradeEffects();
        UpdateUI();
    }


    public void UpdateGemUI()
    {
        int totalGems = GemManager.Instance.GetGemsPermanent();
        totalGemsText.text = " " + totalGems;
    }

    void UpdateUI()
    {
        for (int i = 0; i < upgradeIds.Length; i++)
        {
            var node = tree.Search(upgradeIds[i]);
            if (node == null) continue;

            bool puedeDesbloquearse = false;
            if (node == tree.Root)
                puedeDesbloquearse = !node.Unlocked;
            else
            {
                var parent = tree.GetParent(node.Id);
                puedeDesbloquearse = parent != null && parent.Unlocked && !node.Unlocked;
            }

            upgradeButtons[i].interactable = puedeDesbloquearse;
            upgradeButtons[i].image.color = node.Unlocked ? Color.green : Color.white;
        }
    }


    void ApplyAllUpgradeEffects()
    {
        GameStats.GoldPerKill = 0;
        GameStats.TurretDamageMultiplier = 1f;
        GameStats.TurretRangeMultiplier = 1f;

        foreach (var node in tree.InOrderTraversal())
        {
            if (node.Unlocked)
            {
                switch (node.Id)
                {
                    case 10: GameStats.GoldPerKill = 1; break;
                    case 5: GameStats.TurretDamageMultiplier += 0.10f; break;
                    case 15: GameStats.TurretRangeMultiplier += 0.10f; break;
                    case 12:
                        GameStats.TurretDamageMultiplier += 0.05f;
                        GameStats.TurretRangeMultiplier += 0.05f;
                        break;
                }
            }
        }

        foreach (Turret turret in FindObjectsOfType<Turret>())
        {
            if (turret.turretData != null)
                turret.ApplyData(turret.turretData);
        }
    }

    [ContextMenu("Resetear árbol de mejoras")]
    public void ResetTreeFromInspector()
    {
        foreach (var id in upgradeIds)
            PlayerPrefs.SetInt("Upgrade_" + id, 0);

        tree = new UpgradeTree();
        UpdateUI();
        Debug.Log("Árbol de mejoras reseteado.");
    }
}

