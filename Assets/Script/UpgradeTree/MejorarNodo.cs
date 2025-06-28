using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MejoraNodo
{
    public string nombre;
    public bool desbloqueada;
    public MejoraNodo izquierda;
    public MejoraNodo derecha;

    public MejoraNodo(string nombre)
    {
        this.nombre = nombre;
        this.desbloqueada = false;
        this.izquierda = null;
        this.derecha = null;
    }
}

