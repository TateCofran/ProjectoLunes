using UnityEngine;
using TMPro;
public class WinLoseScreenUI : MonoBehaviour
{
    public TMP_Text totalGemsText; 

    void Start()
    {
        int total = GemManager.Instance.GetGemsPermanent();
        totalGemsText.text = "Gemas Totales: " + total;
    }
}
