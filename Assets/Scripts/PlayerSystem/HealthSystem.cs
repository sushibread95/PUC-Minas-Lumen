using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance { get; private set; }

    // Evento estático para avisar o sistema quando um inimigo morre
    public static event System.Action OnEnemyKilled;

    [Header("UI References (Via HUDManager)")]
    private Slider healthBar;
    private Slider manaBar;

    [Header("Current Status")]
    public float currentHealth;
    public float currentMana;

    [Header("Identity")]
    private PlayerControllerSystem playerController;
    public ProjectileOwner ownerType = ProjectileOwner.None;

    [Header("Bleed Out")]
    public bool isDead = false;
    [SerializeField] private Slider bleedOutSlider;
    [SerializeField] private float bleedOutDuration = 5f;
    private float bleedOutTimer = 0f;
    private bool isInBleedOut = false;

    private CorruptedNPC corruptedNPC;

    private void Awake()
    {
        playerController = GetComponent<PlayerControllerSystem>();
        TryGetComponent(out corruptedNPC);

        if (playerController != null)
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }
    }

    private void Start()
    {
        // Pega valores iniciais do PlayerStats se disponível
        if (playerController != null && PlayerStats.Instance != null)
        {
            currentHealth = PlayerStats.Instance.maxHealth;
            currentMana = PlayerStats.Instance.maxMana;
        }
        else if (corruptedNPC != null)
        {
            // Valores padrão para inimigos
            currentHealth = 100f;
        }

        // Conecta com a UI
        if (HUDManager.Instance != null && playerController != null)
        {
            HUDManager.Instance.AssignSlidersTo(this);
        }
    }

    public void SetSliders(Slider hp, Slider mana, Slider bleed)
    {
        this.healthBar = hp;
        this.manaBar = mana;
        this.bleedOutSlider = bleed;
        UpdateUIBars();
    }

    public void UpdateMaxStats(float newMaxHealth, float newMaxMana)
    {
        // Cura total ao subir de nível (opcional)
        currentHealth = newMaxHealth;
        currentMana = newMaxMana;
        UpdateUIBars();
    }

    private void UpdateUIBars()
    {
        if (playerController == null) return;

        float maxH = (PlayerStats.Instance != null) ? PlayerStats.Instance.maxHealth : 100f;
        float maxM = (PlayerStats.Instance != null) ? PlayerStats.Instance.maxMana : 50f;

        if (healthBar)
        {
            healthBar.maxValue = maxH;
            healthBar.value = currentHealth;
        }
        if (manaBar)
        {
            manaBar.maxValue = maxM;
            manaBar.value = currentMana;
        }
    }

    private void Update()
    {
        if (isDead) return;

        UpdateUIBars();

        // Lógica de Morte / Nocaute
        if (currentHealth <= 0f && !isInBleedOut)
        {
            if (corruptedNPC != null)
            {
                if (corruptedNPC.currentState == NPCState.Corrompido) corruptedNPC.EntrarEmNocaute();
                else if (corruptedNPC.currentState != NPCState.Nocauteado) Destroy(gameObject);
            }
            else if (playerController != null) // Player
            {
                isInBleedOut = true;
                if (bleedOutSlider) bleedOutSlider.gameObject.SetActive(true);
                bleedOutTimer = bleedOutDuration;
            }
        }

        // Lógica de Sangramento do Player
        if (isInBleedOut)
        {
            bleedOutTimer -= Time.deltaTime;
            if (bleedOutSlider) bleedOutSlider.value = bleedOutTimer / bleedOutDuration;

            if (bleedOutTimer <= 0f)
            {
                isDead = true;
                Kill();
            }
        }
    }

    // --- FUNÇÃO RESTAURADA: Usada pelo HealEffect ---
    public void RecoverHealth(float amount)
    {
        float maxH = (PlayerStats.Instance != null) ? PlayerStats.Instance.maxHealth : 100f;

        currentHealth += amount;

        if (currentHealth > maxH)
        {
            currentHealth = maxH;
        }

        UpdateUIBars();
    }
    // ------------------------------------------------

    public void RestoreMana(float amount)
    {
        float maxM = (PlayerStats.Instance != null) ? PlayerStats.Instance.maxMana : 50f;
        currentMana += amount;
        if (currentMana > maxM) currentMana = maxM;
        UpdateUIBars();
    }

    public bool ApplyEffect(Effect[] effectToApply)
    {
        // 1. Checagem de Custo (Mana)
        foreach (var effect in effectToApply)
        {
            if (effect.effectType == Effect.EffectType.magic)
            {
                if (currentMana < effect.power) return false;
            }
        }

        // 2. Aplicação
        foreach (var effect in effectToApply)
        {
            float finalPower = effect.power;

            // Aplicação de Defesa
            if (effect.effectType == Effect.EffectType.physical && playerController != null && PlayerStats.Instance != null)
            {
                finalPower = Mathf.Max(1f, finalPower - PlayerStats.Instance.defense);
            }

            switch (effect.effectType)
            {
                case Effect.EffectType.physical:
                    currentHealth -= finalPower;
                    break;

                case Effect.EffectType.magic: // Custo de Mana
                    currentMana -= finalPower;
                    break;
            }
        }
        return true;
    }

    private void Kill()
    {
        if (playerController != null)
        {
            Debug.Log("JOGADOR MORREU PERMANENTEMENTE.");
            if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public bool CheckEffect(Effect[] effectToApply)
    {
        if (effectToApply == null) return true;

        for (int i = 0; i < effectToApply.Length; i++)
        {
            switch (effectToApply[i].effectType)
            {
                case Effect.EffectType.magic: // Custo de Mana
                    if (currentMana < effectToApply[i].power)
                    {
                        return false; // Mana insuficiente!
                    }
                    break;

                    /*
                    case Effect.EffectType.stamina:
                        if (currentStamina < effectToApply[i].power) return false;
                        break;
                    */
            }
        }
        return true; // Passou em todos os testes
    }

    // --- FUNÇÃO RESTAURADA: Usada pelo EnemyHealth ---
    public static void TriggerEnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }
    // ------------------------------------------------
}