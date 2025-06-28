using UnityEngine;
using System.Collections.Generic;

public class TurretDatabase : MonoBehaviour
{
    public static TurretDatabase Instance;

    public Dictionary<string, TurretData> turretDict = new Dictionary<string, TurretData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadTurretsFromJSON();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void LoadTurretsFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Torretas");
        if (jsonFile != null)
        {
            string json = "{\"turrets\":" + jsonFile.text + "}"; // Envolver en un objeto para deserializar array
            TurretDataList list = JsonUtility.FromJson<TurretDataList>(json);

            foreach (TurretData data in list.turrets)
            {
                turretDict[data.id] = data;
            }

            Debug.Log("Loaded " + list.turrets.Length + " turrets from JSON.");
        }
        else
        {
            Debug.LogError("Not find Torretas.json en Resources.");
        }
    }

    public TurretData GetTurretData(string id)
    {
        if (turretDict.TryGetValue(id, out var data))
            return data;

        Debug.LogWarning("Not find turret with ID: " + id);
        return null;
    }
}
