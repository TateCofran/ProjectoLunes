using System.Collections.Generic;
using UnityEngine;

public class TileParentInfo : MonoBehaviour
{
    public int tileIndex;
    public List<int> vertices = new List<int>();
    public List<(int from, int to, int peso)> aristas = new List<(int, int, int)>();
}
