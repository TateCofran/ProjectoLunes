using UnityEngine;
using TMPro;
using System.Collections;

public class Core : MonoBehaviour
{
    public int maxHealth = 3;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        //Debug.Log("Core health: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("¡El núcleo fue destruido!");
            GameManager.Instance.OnCoreDestroyed();
        }
    }
}
