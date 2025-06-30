using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeTreeUI : MonoBehaviour
{
    [Header("Oro al matar")]
    public Button btnOroAlMatar;
    public TMP_Text txtOroAlMatarNombre;
    public TMP_Text txtOroAlMatarDescCost;
    public TMP_Text txtOroAlMatarReq;

    [Header("Más daño")]
    public Button btnMasDano;
    public TMP_Text txtMasDanoNombre;
    public TMP_Text txtMasDanoDescCost;
    public TMP_Text txtMasDanoReq;

    [Header("Más alcance")]
    public Button btnMasAlcance;
    public TMP_Text txtMasAlcanceNombre;
    public TMP_Text txtMasAlcanceDescCost;
    public TMP_Text txtMasAlcanceReq;

    [Header("Mejora global")]
    public Button btnMejoraGlobal;
    public TMP_Text txtMejoraGlobalNombre;
    public TMP_Text txtMejoraGlobalDescCost;
    public TMP_Text txtMejoraGlobalReq;

    private UpgradeTree tree;

    [Header("Gems")]
    public TMP_Text totalGemsText;

    void Start()
    {
        tree = new UpgradeTree();
        UpdateGemUI();
        LoadUpgrades(tree.Root);

        txtOroAlMatarNombre.text = tree.Root.Name;
        txtOroAlMatarDescCost.text = tree.Root.Description + "\nCosto: " + tree.Root.Cost;

        txtMasDanoNombre.text = tree.Root.Left.Name;
        txtMasDanoDescCost.text = tree.Root.Left.Description + "\nCosto: " + tree.Root.Left.Cost;

        txtMasAlcanceNombre.text = tree.Root.Right.Name;
        txtMasAlcanceDescCost.text = tree.Root.Right.Description + "\nCosto: " + tree.Root.Right.Cost;

        txtMejoraGlobalNombre.text = tree.Root.Left.Right.Name;
        txtMejoraGlobalDescCost.text = tree.Root.Left.Right.Description + "\nCosto: " + tree.Root.Left.Right.Cost;

        btnOroAlMatar.onClick.AddListener(() => TryUnlock(tree.Root));
        btnMasDano.onClick.AddListener(() => TryUnlock(tree.Root.Left));
        btnMasAlcance.onClick.AddListener(() => TryUnlock(tree.Root.Right));
        btnMejoraGlobal.onClick.AddListener(() => TryUnlock(tree.Root.Left.Right));

        ApplyAllUpgradeEffects();
        UpdateUI();
    }

    void TryUnlock(UpgradeNode node)
    {
        if (node.Unlocked)
            return;

        string requisito = tree.GetMissingRequirement(node);
        if (!string.IsNullOrEmpty(requisito))
        {
            Debug.Log("Te falta: " + requisito);
            return;
        }

        if (!GemManager.Instance.SpendGemsPermanent(node.Cost))
        {
            Debug.Log("No tenés suficientes gemas permanentes.");
            return;
        }

        tree.Unlock(node);
        SaveUpgrade(node);

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
        txtOroAlMatarReq.text = tree.GetMissingRequirement(tree.Root);
        txtMasDanoReq.text = tree.GetMissingRequirement(tree.Root.Left);
        txtMasAlcanceReq.text = tree.GetMissingRequirement(tree.Root.Right);
        txtMejoraGlobalReq.text = tree.GetMissingRequirement(tree.Root.Left.Right);

        btnOroAlMatar.interactable = !tree.Root.Unlocked;
        btnMasDano.interactable = tree.Root.Unlocked && !tree.Root.Left.Unlocked;
        btnMasAlcance.interactable = tree.Root.Unlocked && !tree.Root.Right.Unlocked;
        btnMejoraGlobal.interactable = tree.Root.Left.Unlocked && tree.Root.Right.Unlocked && !tree.Root.Left.Right.Unlocked;

        btnOroAlMatar.image.color = tree.Root.Unlocked ? Color.green : Color.white;
        btnMasDano.image.color = tree.Root.Left.Unlocked ? Color.green : Color.white;
        btnMasAlcance.image.color = tree.Root.Right.Unlocked ? Color.green : Color.white;
        btnMejoraGlobal.image.color = tree.Root.Left.Right.Unlocked ? Color.green : Color.white;
    }
    public void SaveUpgrade(UpgradeNode node)
    {
        PlayerPrefs.SetInt("Upgrade_" + node.Name, node.Unlocked ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadUpgrades(UpgradeNode node)
    {
        if (node == null) return;
        node.Unlocked = PlayerPrefs.GetInt("Upgrade_" + node.Name, 0) == 1;
        LoadUpgrades(node.Left);
        LoadUpgrades(node.Right);
    }
    void ApplyAllUpgradeEffects()
    {
        GameStats.GoldPerKill = 0;
        GameStats.TurretDamageMultiplier = 1f;
        GameStats.TurretRangeMultiplier = 1f;

        if (tree.Root.Unlocked)
            GameStats.GoldPerKill = 1;
        if (tree.Root.Left.Unlocked)
            GameStats.TurretDamageMultiplier += 0.10f;
        if (tree.Root.Right.Unlocked)
            GameStats.TurretRangeMultiplier += 0.10f;
        if (tree.Root.Left.Right.Unlocked)
        {
            GameStats.TurretDamageMultiplier += 0.05f;
            GameStats.TurretRangeMultiplier += 0.05f;
        }

        // ACTUALIZAR todas las torretas en escena
        foreach (Turret turret in FindObjectsOfType<Turret>())
        {
            if (turret.turretData != null)
                turret.ApplyData(turret.turretData);
        }
    }
    [ContextMenu("Resetear árbol de mejoras")]
    public void ResetTreeFromInspector()
    {
        tree = new UpgradeTree();
        UpdateUI();
        Debug.Log("Árbol de mejoras reseteado.");
    }
}
