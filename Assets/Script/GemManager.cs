using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance;

    [Header("Gemas de esta partida (run actual)")]
    public int gemsRun = 0;   

    [Header("Gemas permanentes (acumuladas)")]
    private int gemsPermanent = 0; 

    public TMP_Text gemsRunText;
    public TMP_Text gemsPermanentText;

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
        UpdateUI();

    }

    private void Start()
    {
        gemsPermanent = PlayerPrefs.GetInt("GemsPermanent", 0);
        UpdateUI();
    }

    public void AddGemsRun(int amount)
    {
        gemsRun += amount;
        UpdateUI();
    }

    public bool SpendGemsRun(int amount)
    {
        if (gemsRun >= amount)
        {
            gemsRun -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void SaveRunToPermanent()
    {
        gemsPermanent += gemsRun;
        PlayerPrefs.SetInt("GemsPermanent", gemsPermanent);
        PlayerPrefs.Save();
        gemsRun = 0;
        UpdateUI();
    }

   
    public bool SpendGemsPermanent(int amount)
    {
        if (gemsPermanent >= amount)
        {
            gemsPermanent -= amount;
            PlayerPrefs.SetInt("GemsPermanent", gemsPermanent);
            PlayerPrefs.Save();
            UpdateUI();
            return true;
        }
        return false;
    }

    public int GetGemsRun()
    {
        return gemsRun;
    }

    public int GetGemsPermanent()
    {
        return PlayerPrefs.GetInt("GemsPermanent", 0);
    }

    private void UpdateUI()
    {
        if (gemsRunText != null)
            gemsRunText.text = gemsRun.ToString();
        if (gemsPermanentText != null)
            gemsPermanentText.text = gemsPermanent.ToString();
    }

    [ContextMenu("Agregar 10 gemas permanentes")]
    public void Add10GemsPermanentFromInspector()
    {
        gemsPermanent += 10;
        PlayerPrefs.SetInt("GemsPermanent", gemsPermanent);
        PlayerPrefs.Save();
        UpdateUI();
        Debug.Log("+10 gemas permanentes!");
    }
}
