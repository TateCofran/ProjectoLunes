using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MejoraTorretaUI : MonoBehaviour
{
    public Turret miTorreta;                    // Arrastrás la torreta de la escena o la buscás por código
    public Button[] botonesMejora;
    public TextMeshProUGUI[] textosMejora;

    private MejoraNodo[] nodos;

    void Start()
    {
        nodos = new MejoraNodo[botonesMejora.Length];
        nodos[0] = miTorreta.arbolDeMejoras.raiz;
        nodos[1] = miTorreta.arbolDeMejoras.raiz.izquierda;
        nodos[2] = miTorreta.arbolDeMejoras.raiz.derecha;
        // Agregá más según el árbol

        for (int i = 0; i < botonesMejora.Length; i++)
        {
            int idx = i;
            botonesMejora[i].onClick.AddListener(() => OnMejoraClick(idx));
            ActualizarTexto(idx);
        }
    }

    void OnMejoraClick(int index)
    {
        MejoraNodo nodo = nodos[index];
        bool result = false;

        if (miTorreta is GoldTurret gold)
            result = gold.TryApplyGoldUpgrade(nodo);
        else
            result = miTorreta.TryApplyUpgrade(nodo);

        if (result)
        {
            botonesMejora[index].interactable = false;
            ActualizarTexto(index);
        }
        else
        {
            Debug.Log("No se puede desbloquear: " + nodo.nombre);
        }
    }

    void ActualizarTexto(int index)
    {
        if (nodos[index].desbloqueada)
            textosMejora[index].text = nodos[index].nombre + " <color=green>(Desbloqueada)</color>";
        else
            textosMejora[index].text = nodos[index].nombre;
    }
}
