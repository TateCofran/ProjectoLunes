using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPila<T>
{
    void InicializarPila();
    void Apilar(T x);
    void Desapilar();
    bool PilaVacia();
    T Tope();
    int Count();
}

