using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyCounterUI : MonoBehaviour
{
    [Header("Referencia al TextMeshPro")]
    public TMP_Text counterText;

    void Update()
    {
        // Cuenta cuántos Enemy activos hay en escena
        int alive = FindObjectsOfType<Enemy>().Length;
        counterText.text = "Remaining Enemies: " + alive;
    }
}
