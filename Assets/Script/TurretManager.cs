using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject turretUIPrefab;
    public Transform turretsPanel;

    private List<Turret> activeTurrets = new List<Turret>();
    public static TurretManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    public void RegisterTurret(Turret turret)
    {
        activeTurrets.Add(turret);
        RefreshUI();
    }

    public void UnregisterTurret(Turret turret)
    {
        if (activeTurrets.Contains(turret))
        {
            activeTurrets.Remove(turret);
            Destroy(turret.gameObject);
            RefreshUI();
        }
    }

    public void RefreshUI()
    {
        foreach (Transform child in turretsPanel)
            Destroy(child.gameObject);

        foreach (var turret in activeTurrets)
        {
            activeTurrets.RemoveAll(t => t == null);

            GameObject uiItem = Instantiate(turretUIPrefab, turretsPanel);

            Transform nombreTransform = uiItem.transform.Find("Left/NameTxt");
            Transform nivelTransform = uiItem.transform.Find("Left/LvlTxt");
            Transform botonMejorarTransform = uiItem.transform.Find("Right/LvlUpButton");
            Transform botonEliminarTransform = uiItem.transform.Find("Right/SellButton");

            if (nombreTransform == null || nivelTransform == null || botonMejorarTransform == null || botonEliminarTransform == null)
            {
                Debug.LogError("Uno o más objetos hijos del prefab TurretUIPrefab no están correctamente nombrados o faltan.");
                continue; // evita el error continuando al siguiente elemento
            }

            TextMeshProUGUI nombreTexto = nombreTransform.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI nivelTexto = nivelTransform.GetComponent<TextMeshProUGUI>();
            Button botonMejorar = botonMejorarTransform.GetComponent<Button>();
            Button botonEliminar = botonEliminarTransform.GetComponent<Button>();

            nombreTexto.text = turret.turretName;
            nivelTexto.text = "Nivel: " + turret.upgradeLevel;

            TurretInfoUI turretInfoUI = turret.GetComponent<TurretInfoUI>();

            // Verificamos si es torreta de soporte
            bool isSupport = turret.turretData != null && turret.turretData.type == "support";

            // Ocultar o mostrar botones según el tipo
            botonMejorar.gameObject.SetActive(!isSupport);
            botonEliminar.gameObject.SetActive(true); // Se puede vender cualquiera

            if (!isSupport)
            {
                botonMejorar.onClick.AddListener(() => {
                    if (turretInfoUI != null)
                    {
                        turretInfoUI.UpgradeTurret();
                        RefreshUI();
                    }
                });
            }

            botonEliminar.onClick.AddListener(() => {
                if (turretInfoUI != null)
                {
                    turretInfoUI.SellTurret();
                    UnregisterTurret(turret);
                }
            });
        }
    }

}