
using UnityEngine;

public class GoldTurret : Turret
{
    private bool registered = false;
    private int oroExtra = 0; // oro extra ganado por las mejoras

    protected override void Start()
    {
        // Es un árbol especial para gold
        arbolDeMejoras = new ArbolDeMejoras(true);
        nodoActual = arbolDeMejoras.raiz;
        nodoActual.desbloqueada = true;

        base.Start();
        RegisterToWaveSpawner();
    }

    // Para la UI, llamar esto en vez de TryApplyUpgrade
    public bool TryApplyGoldUpgrade(MejoraNodo nodo)
    {
        if (arbolDeMejoras.Desbloquear(nodo))
        {
            AplicarMejoraGold(nodo);
            nodoActual = nodo;
            upgradeLevel++;
            return true;
        }
        return false;
    }

    private void AplicarMejoraGold(MejoraNodo nodo)
    {
        if (nodo.oroExtraPorRonda > 0)
            oroExtra += nodo.oroExtraPorRonda;
    }

    public void RegisterToWaveSpawner()
    {
        if (!registered && WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.RegisterGoldTurret(this);
            registered = true;
        }
    }

    // Sobrescribe GiveGold para sumar el bonus extra
    public void GiveGold()
    {
        if (turretData != null)
        {
            int amount = turretData.goldPerWave + oroExtra;
            GoldManager.Instance.AddGold(amount);
            Debug.Log("Gold turret otorgó " + amount + " de oro.");
        }
    }
}


