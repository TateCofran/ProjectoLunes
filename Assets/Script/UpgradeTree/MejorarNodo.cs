using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MejoraNodo
{
    public string nombre;
    public bool desbloqueada;
    public MejoraNodo izquierda;
    public MejoraNodo derecha;
    // Campo extra para upgrades de oro (opcional, si lo necesitás)
    public int oroExtraPorRonda = 0;  // SOLO para GoldTurret
    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;
    public float fireRateMultiplier = 1f;

    public MejoraNodo(string nombre)
    {
        this.nombre = nombre;
        this.desbloqueada = false;
        this.izquierda = null;
        this.derecha = null;
    }
}


