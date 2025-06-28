using UnityEngine;

public class GoldTurret : Turret
{
    private bool registered = false;

    protected override void Start()
    {
        base.Start();
        RegisterToWaveSpawner();
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
            int amount = turretData.goldPerWave;
            GoldManager.Instance.AddGold(amount);
            Debug.Log("Gold turret otorgó " + amount + " de oro.");
        }
    }
}

