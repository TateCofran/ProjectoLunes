using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Config")]
    public int maxWaves = 15;

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
    }

    public void OnCoreDestroyed()
    {
        GemManager.Instance.SaveRunToPermanent();
        SceneManager.LoadScene("LoseScreen");
    }

    public void OnVictory()
    {
        GemManager.Instance.SaveRunToPermanent();
        SceneManager.LoadScene("WinScreen");
    }
}
