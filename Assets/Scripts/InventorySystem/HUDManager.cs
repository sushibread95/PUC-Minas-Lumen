using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Referências Reais")]
    public Image healthBar;
    public Image manaBar;
    public Image bleedOutBar;
    // Removidos hurtHealthBar e oneShotProtectionSlider

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void AssignBarsTo(HealthSystem playerHealth)
    {
        playerHealth.SetBars(healthBar, manaBar, bleedOutBar);
    }
}