using UnityEngine;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    private HashSet<string> mejorasDesbloqueadas = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CargarMejoras();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void DesbloquearMejora(string torretaID, string mejoraID)
    {
        string key = torretaID + "_" + mejoraID;
        if (!mejorasDesbloqueadas.Contains(key))
        {
            mejorasDesbloqueadas.Add(key);
            PlayerPrefs.SetInt("Mejora_" + key, 1);
            PlayerPrefs.Save();
        }
    }

    public bool EstaDesbloqueada(string torretaID, string mejoraID)
    {
        string key = torretaID + "_" + mejoraID;
        return mejorasDesbloqueadas.Contains(key);
    }

    void CargarMejoras()
    {
        // Si sabés todas tus combinaciones, podés recorrerlas. 
        // O simplemente podés cargar en tiempo de ejecución como abajo.
        // Ejemplo: "Normal_damage", "Normal_range", "Gold_oro"
        string[] torretas = { "Normal", "Gold" };
        string[] mejorasNormal = { "oroMatar", "damage", "range", "stats" };
        string[] mejorasGold = { "oro5", "oro10", "oro20" };

        foreach (var t in torretas)
        {
            string[] mejoras = t == "Gold" ? mejorasGold : mejorasNormal;
            foreach (var m in mejoras)
            {
                string key = t + "_" + m;
                if (PlayerPrefs.GetInt("Mejora_" + key, 0) == 1)
                    mejorasDesbloqueadas.Add(key);
            }
        }
    }
}
