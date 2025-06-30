using UnityEngine;
using TMPro;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;

    [Header("UI References")]
    public TMP_Text goldText;   

    [Header("Initial Values")]
    private int currentGold = 100;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        UpdateGoldUI();
    }
    private void Start()
    {
        currentGold = 100;
    }
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

    public void ResetGold()
    {
        currentGold = 100; 
        UpdateGoldUI();
    }
    void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"Oro: {currentGold}";
    }
}
