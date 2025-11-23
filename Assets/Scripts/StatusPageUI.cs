using UnityEngine;
using TMPro;

public class StatusPageUI : MonoBehaviour
{
    [Header("Textos de Atributos")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI attackText;
    public TextMeshProUGUI defenseText;

    [Header("Textos de XP")]
    public TextMeshProUGUI xpText; // Ex: 150 / 300

    // Roda toda vez que o GameObject é ativado (muda de aba)
    void OnEnable()
    {
        UpdateStats();
    }

    public void UpdateStats()
    {
        if (PlayerStats.Instance == null) return;

        if (levelText) levelText.text = PlayerStats.Instance.level.ToString();

        // Mostra "Atual / Max"
        if (healthText && HealthSystem.Instance != null)
            healthText.text = $"{HealthSystem.Instance.currentHealth:F0} / {PlayerStats.Instance.maxHealth:F0}";

        if (manaText && HealthSystem.Instance != null)
            manaText.text = $"{HealthSystem.Instance.currentMana:F0} / {PlayerStats.Instance.maxMana:F0}";

        if (attackText) attackText.text = PlayerStats.Instance.physicalAttack.ToString("F0");
        if (defenseText) defenseText.text = PlayerStats.Instance.defense.ToString("F0");

        if (xpText && LevelingSystem.Instance != null)
        {
            xpText.text = $"{LevelingSystem.Instance.currentBaseXP:F0} / {LevelingSystem.Instance.xpToNextLevel:F0}";
        }
    }
}