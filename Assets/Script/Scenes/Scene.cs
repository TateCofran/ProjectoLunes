using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("Level");
    }
    public void Exit()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
    public void GoToMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void Retry()
    {
        SceneManager.LoadScene("Level");
    }

    public void Upgrades()
    {
        SceneManager.LoadScene("UpgradeScene");
    }
}
