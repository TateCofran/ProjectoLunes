using UnityEngine;
using TMPro;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;

    public int currentGold = 30;
    public TMP_Text goldText;

    void Awake()
    {
        Instance = this;
        UpdateGoldUI();
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

    void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"Oro: {currentGold}";
    }
}
