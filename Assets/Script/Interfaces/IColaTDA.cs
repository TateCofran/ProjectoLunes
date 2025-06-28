using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICola
{
    void InicializarCola();

    void Acolar(int x);

    void Desacolar();

    bool ColaVacia();

    int Primero();
}

