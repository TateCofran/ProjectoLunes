using UnityEngine;
using TMPro;

public class PermanentGoldManager : MonoBehaviour
{
    public static PermanentGoldManager Instance;
    public TMP_Text permanentGoldText;
    private int permanentGold = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            permanentGold = PlayerPrefs.GetInt("PermanentGold", 0);
        }
        else
            Destroy(gameObject);

        UpdatePermanentGoldUI();
    }

    public void AddPermanentGold(int amount)
    {
        permanentGold += amount;
        PlayerPrefs.SetInt("PermanentGold", permanentGold);
        PlayerPrefs.Save();
        UpdatePermanentGoldUI();
    }

    public int GetPermanentGold() => permanentGold;

    void UpdatePermanentGoldUI()
    {
        if (permanentGoldText != null)
            permanentGoldText.text = $"Oro permanente: {permanentGold}";
    }
}
