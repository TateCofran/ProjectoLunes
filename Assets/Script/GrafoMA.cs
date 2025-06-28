using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Script
{
    public interface ConjuntoTDA
    {
        void InicializarConjunto();
        bool ConjuntoVacio();
        void Agregar(int x);
        int Elegir();
        void Sacar(int x);
        bool Pertenece(int x);
    }

    public class Nodo
    {
        public int info;
        public Nodo sig;
    }
    public interface GrafoTDA
    {
        void InicializarGrafo();
        void AgregarVertice(int v);
        void EliminarVertice(int v);
        ConjuntoTDA Vertices();
        void AgregarArista(int id, int v1, int v2, int peso);
        void EliminarArista(int v1, int v2);
        bool ExisteArista(int v1, int v2);
        int PesoArista(int v1, int v2);
    }

    public class GrafoMA : GrafoTDA
    {
        static int n = 10000;
        public int[,] MAdy;
        public int[,] MId;
        public int[] Etiqs;
        public int cantNodos;

        public void InicializarGrafo()
        {
            MAdy = new int[n, n];
            MId = new int[n, n];
            Etiqs = new int[n];
            cantNodos = 0;
        }

        public void AgregarVertice(int v)
        {
            if (cantNodos >= Etiqs.Length)
            {
                // Duplicar tamaño de Etiqs y de matrices de adyacencia
                int nuevoTam = Etiqs.Length * 2;
                System.Array.Resize(ref Etiqs, nuevoTam);
                int[,] nuevaMAdy = new int[nuevoTam, nuevoTam];
                int[,] nuevaMId = new int[nuevoTam, nuevoTam];
                for (int i = 0; i < MAdy.GetLength(0); i++)
                    for (int j = 0; j < MAdy.GetLength(1); j++)
                    {
                        nuevaMAdy[i, j] = MAdy[i, j];
                        nuevaMId[i, j] = MId[i, j];
                    }
                MAdy = nuevaMAdy;
                MId = nuevaMId;
            }

            Etiqs[cantNodos] = v;
            cantNodos++;
        }


        public void EliminarVertice(int v)
        {
            int ind = Vert2Indice(v);

            for (int k = 0; k < cantNodos; k++)
            {
                MAdy[k, ind] = MAdy[k, cantNodos - 1];
            }

            for (int k = 0; k < cantNodos; k++)
            {
                MAdy[ind, k] = MAdy[cantNodos - 1, k];
            }

            Etiqs[ind] = Etiqs[cantNodos - 1];
            cantNodos--;
        }

        public int Vert2Indice(int v)
        {
            int i = cantNodos - 1;
            while (i >= 0 && Etiqs[i] != v)
            {
                i--;
            }

            return i;
        }

        public ConjuntoTDA Vertices()
        {
            ConjuntoTDA Vert = new ConjuntoLD();
            Vert.InicializarConjunto();
            for (int i = 0; i < cantNodos; i++)
            {
                Vert.Agregar(Etiqs[i]);
            }
            return Vert;
        }

        public void AgregarArista(int id, int v1, int v2, int peso)
        {
            int o = Vert2Indice(v1);
            int d = Vert2Indice(v2);
            MAdy[o, d] = peso;
            MId[o, d] = id;
        }

        public void EliminarArista(int v1, int v2)
        {
            int o = Vert2Indice(v1);
            int d = Vert2Indice(v2);
            MAdy[o, d] = 0;
            MId[o, d] = 0;
        }

        public bool ExisteArista(int v1, int v2)
        {
            int o = Vert2Indice(v1);
            int d = Vert2Indice(v2);
            return MAdy[o, d] != 0;
        }

        public int PesoArista(int v1, int v2)
        {
            int o = Vert2Indice(v1);
            int d = Vert2Indice(v2);
            return MAdy[o, d];
        }
    }
    public class ConjuntoLD : ConjuntoTDA
    {
        Nodo c;
        public void InicializarConjunto()
        {
            c = null;

        }
        public bool ConjuntoVacio()
        {
            return (c == null);
        }
        public void Agregar(int x)
        {
            /* Verifica que x no este en el conjunto */
            if (!this.Pertenece(x))
            {
                Nodo aux = new Nodo();
                aux.info = x;
                aux.sig = c;
                c = aux;
            }
        }
        public int Elegir()
        {
            return c.info;
        }
        public void Sacar(int x)
        {
            if (c != null)
            {
                // si es el primer elemento de la lista
                if (c.info == x)
                {
                    c = c.sig;
                }
                else
                {
                    Nodo aux = c;
                    while (aux.sig != null && aux.sig.info != x)
                        aux = aux.sig;
                    if (aux.sig != null)
                        aux.sig = aux.sig.sig;
                }
            }
        }
        public bool Pertenece(int x)
        {
            Nodo aux = c;
            while ((aux != null) && (aux.info != x))
            {
                aux = aux.sig;
            }
            return (aux != null);
        }
    }
}
