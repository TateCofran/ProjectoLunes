
using UnityEngine;

public class GoldTurret : Turret
{
    private bool registered = false;
    private int oroExtra = 0; 

    protected override void Start()
    {
        arbolDeMejoras = new ArbolDeMejoras(true);
        nodoActual = arbolDeMejoras.raiz;
        nodoActual.desbloqueada = true;
        torretaID = "Gold";

        AplicarMejorasGuardadas();
        base.Start();
        RegisterToWaveSpawner();
    }
    void AplicarMejorasGuardadas()
    {
        // "oro5", "oro10", "oro20"
        if (UpgradeManager.Instance.EstaDesbloqueada(torretaID, "oro5"))
            oroExtra += arbolDeMejoras.raiz.oroExtraPorRonda;

        if (UpgradeManager.Instance.EstaDesbloqueada(torretaID, "oro10"))
            oroExtra += arbolDeMejoras.raiz.izquierda.oroExtraPorRonda;

        if (UpgradeManager.Instance.EstaDesbloqueada(torretaID, "oro20"))
            oroExtra += arbolDeMejoras.raiz.derecha.oroExtraPorRonda;
    }

    public bool TryApplyGoldUpgrade(MejoraNodo nodo)
    {
        if (nodo == null) return false;

        if (arbolDeMejoras.Desbloquear(nodo))
        {
            if (nodo.oroExtraPorRonda > 0)
                oroExtra += nodo.oroExtraPorRonda;
            nodoActual = nodo;
            return true;
        }
        return false;
    }

    public void RegisterToWaveSpawner()
    {
        if (!registered && WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.RegisterGoldTurret(this);
            registered = true;
        }
    }

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


