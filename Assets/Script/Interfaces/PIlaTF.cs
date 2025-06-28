using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PilaTF<T> : IPila<T>
{
    private T[] a;
    private int indice;
    private const int capacidadInicial = 100;

    public void InicializarPila()
    {
        a = new T[capacidadInicial];
        indice = 0;
    }

    public void Apilar(T x)
    {
        if (indice >= a.Length)
        {
            Array.Resize(ref a, a.Length * 2);
        }
        a[indice] = x;
        indice++;
    }

    public void Desapilar()
    {
        if (!PilaVacia())
        {
            indice--;
        }
        else
        {
            throw new InvalidOperationException("La pila está vacía. No se puede desapilar.");
        }
    }

    public bool PilaVacia()
    {
        return (indice == 0);
    }

    public T Tope()
    {
        if (!PilaVacia())
        {
            return a[indice - 1];
        }
        else
        {
            throw new InvalidOperationException("La pila está vacía. No hay tope.");
        }
    }

    public int Count()
    {
        return indice;
    }
}


