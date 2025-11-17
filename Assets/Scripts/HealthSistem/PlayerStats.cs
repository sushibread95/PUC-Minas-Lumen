using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    // Crie uma instância estática para facilitar o acesso
    public static PlayerStats Instance;

    public int currentHealth;
    public int maxHealth = 100;
    // Adicione outros stats aqui (força, defesa, etc.)

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        currentHealth = maxHealth;
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        // TODO: Atualizar a UI de vida
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0)
            currentHealth = 0;

        // TODO: Lógica de morte ou atualizar UI
    }
}