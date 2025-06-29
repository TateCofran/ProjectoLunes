
using UnityEngine;

public class ArbolDeMejoras
{
    public MejoraNodo raiz;

    public ArbolDeMejoras(bool esGoldTurret = false)
    {
        if (esGoldTurret)
        {
            // SOLO mejoras de oro para GoldTurret
            raiz = new MejoraNodo("Oro extra por ronda (+5)");
            raiz.oroExtraPorRonda = 5;

            raiz.izquierda = new MejoraNodo("Oro extra por ronda (+10)");
            raiz.izquierda.oroExtraPorRonda = 10;

            raiz.derecha = new MejoraNodo("Oro extra por ronda (+20)");
            raiz.derecha.oroExtraPorRonda = 20;
        }
        else
        {
            // Mejora normal para torretas comunes
            raiz = new MejoraNodo("Oro al matar (1 por enemigo eliminado)");

            raiz.izquierda = new MejoraNodo("Más daño de las torretas (10% más)");
            raiz.izquierda.damageMultiplier = 1.1f;
            raiz.derecha = new MejoraNodo("Más alcance de las torretas (10% más)");
            raiz.derecha.rangeMultiplier = 1.1f;

            raiz.izquierda.derecha = new MejoraNodo("Aumento de todas las estadísticas en un 5%");
            raiz.izquierda.derecha.damageMultiplier = 1.05f;
            raiz.izquierda.derecha.rangeMultiplier = 1.05f;
            raiz.izquierda.derecha.fireRateMultiplier = 1.05f;
            raiz.derecha.izquierda = raiz.izquierda.derecha; // Ambas ramas apuntan al mismo nodo
        }
    }

    public bool PuedeDesbloquear(MejoraNodo nodo)
    {
        if (nodo == null || nodo.desbloqueada) return false;

        if (nodo == raiz)
            return true;

        if (nodo == raiz.izquierda || nodo == raiz.derecha)
            return raiz.desbloqueada;

        if (nodo == raiz.izquierda.derecha)
            return raiz.izquierda.desbloqueada && raiz.derecha.desbloqueada;

        return false;
    }

    public bool Desbloquear(MejoraNodo nodo)
    {
        if (PuedeDesbloquear(nodo))
        {
            nodo.desbloqueada = true;
            Debug.Log($"Mejora desbloqueada: {nodo.nombre}");
            return true;
        }
        else
        {
            Debug.LogWarning($"No se puede desbloquear: {nodo.nombre}. Falta requisito previo.");
            return false;
        }
    }
}



