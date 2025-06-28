using UnityEngine;
using UnityEngine.UI;

public class RewardPanelManager : MonoBehaviour
{
    public GameObject panel;

    public Button optionMoreEnemiesAndGold;
    public Button optionUpgradeTurrets;
    public Button optionSkipToFinalWave;

    private WaveSpawner waveSpawner;

    void Start()
    {
        waveSpawner = WaveSpawner.Instance;



        panel.SetActive(false); // Ocultar al iniciar
    }

    public void ShowPanel()
    {
        panel.SetActive(true);
    }

    public void HidePanel()
    {
        panel.SetActive(false);
    }


}
