using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColaTF<T>
{
    class Nodo
    {
        public T datos;
        public Nodo siguiente;
    }

    private Nodo primero;
    private Nodo ultimo;
    private int count;

    public void InicializarCola()
    {
        primero = null;
        ultimo = null;
        count = 0;
    }

    public void Acolar(T x)
    {
        Nodo nuevo = new Nodo();
        nuevo.datos = x;
        nuevo.siguiente = null;

        if (ultimo != null)
        {
            ultimo.siguiente = nuevo;
        }
        ultimo = nuevo;

        if (primero == null)
        {
            primero = ultimo;
        }

        count++; 
    }

    public void Desacolar()
    {
        if (primero != null)
        {
            primero = primero.siguiente;
            if (primero == null)
            {
                ultimo = null;
            }
            count--; 
        }
    }

    public bool ColaVacia()
    {
        return (primero == null);
    }

    public T Primero()
    {
        if (ColaVacia())
            throw new InvalidOperationException("La cola está vacía.");
        return primero.datos;
    }

    public int Count() 
    {
        return count;
    }
}


