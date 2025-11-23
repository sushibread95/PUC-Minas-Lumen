using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Referências Reais")]
    public Slider healthBar;
    public Slider manaBar;
    public Slider bleedOutSlider;
    // Removidos hurtHealthBar e oneShotProtectionSlider

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void AssignSlidersTo(HealthSystem playerHealth)
    {
        // Passa apenas os que sobraram
        playerHealth.SetSliders(healthBar, manaBar, bleedOutSlider);
    }
}