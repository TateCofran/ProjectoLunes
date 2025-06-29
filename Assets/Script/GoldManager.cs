using UnityEngine;
using TMPro;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;

    [Header("UI References")]
    public TMP_Text goldText;             // Oro actual
    public TMP_Text permanentGoldText;    // Oro permanente

    [Header("Initial Values")]
    public int startingGold = 30;

    private int currentGold;
    private int permanentGold;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Oro permanente (se guarda en PlayerPrefs)
        permanentGold = PlayerPrefs.GetInt("PermanentGold", 0);

        // Oro corriente
        currentGold = startingGold;

        UpdateGoldUI();
        UpdatePermanentGoldUI();
    }

    // Métodos de oro corriente
    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI();
    }

    public void SpendGold(int amount)
    {
        currentGold -= amount;
        UpdateGoldUI();
    }

    public bool HasEnoughGold(int amount)
    {
        return currentGold >= amount;
    }

    public int GetCurrentGold()
    {
        return currentGold;
    }

    // Métodos de oro permanente
    public void AddPermanentGold(int amount)
    {
        permanentGold += amount;
        PlayerPrefs.SetInt("PermanentGold", permanentGold);
        PlayerPrefs.Save();
        UpdatePermanentGoldUI();
    }

    public int GetPermanentGold()
    {
        return permanentGold;
    }

    // UI
    void UpdateGoldUI()
    {
        if (goldText == null)
        {
            Debug.LogError("GoldManager: goldText ES NULO. Revisa el Inspector.");
            return;
        }

        Debug.Log($"GoldManager: actualizando texto a {currentGold}");
        goldText.text = $"gold: {currentGold}";
    }

    void UpdatePermanentGoldUI()
    {
        if (permanentGoldText != null)
            permanentGoldText.text = $"Oro permanente: {permanentGold}";
    }
}
