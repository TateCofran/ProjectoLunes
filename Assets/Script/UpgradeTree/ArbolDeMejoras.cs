using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArbolDeMejoras
{
    public MejoraNodo raiz;

    public ArbolDeMejoras()
    {
        // Nivel 1
        raiz = new MejoraNodo("Oro al matar (1 por enemigo eliminado)");

        // Nivel 2
        raiz.izquierda = new MejoraNodo("Más daño de las torretas (10% más)");
        raiz.derecha = new MejoraNodo("Más alcance de las torretas (10% más)");

        // Nivel 3
        raiz.izquierda.derecha = new MejoraNodo("Aumento de todas las estadísticas en un 5%");
        raiz.derecha.izquierda = raiz.izquierda.derecha; // Ambas ramas apuntan al mismo nodo
    }

    // Devuelve true si la mejora se puede desbloquear (cumple requisitos previos)
    public bool PuedeDesbloquear(MejoraNodo nodo)
    {
        if (nodo == null || nodo.desbloqueada) return false;

        if (nodo == raiz)
            return true; // Primer nivel siempre disponible

        // Nivel 2: su padre debe estar desbloqueado
        if (nodo == raiz.izquierda || nodo == raiz.derecha)
            return raiz.desbloqueada;

        // Nivel 3: ambas mejoras de nivel 2 desbloqueadas
        if (nodo == raiz.izquierda.derecha)
            return raiz.izquierda.desbloqueada && raiz.derecha.desbloqueada;

        return false;
    }

    // Desbloquea la mejora si se puede
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
