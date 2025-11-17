using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance { get; private set; }
    public static event System.Action OnEnemyKilled;

    [Header("Components")]
    [SerializeField] private Slider maximumHealthBar;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Slider hurtHealthBar;
    [SerializeField] private Slider oneShotProtectionSlider;
    [SerializeField] private Slider bleedOutSlider;

    [Header("Health Attributes")]
    public int health = 100;

    [Header("Stamina/Mana Attributes")]
    public int stamina = 100;
    [SerializeField] private float staminaRecoveryRate = 2f; // Rápida
    [SerializeField] private float manaRecoveryRate = 0.5f;  // Lenta

    // --- INÍCIO DA MUDANÇA 1 ---
    private float currentRecoveryRate; // Armazena a taxa atual
    // --- FIM DA MUDANÇA 1 ---

    [Header("Identity")]
    public ProjectileOwner ownerType = ProjectileOwner.None;
    [HideInInspector] public bool justTookDamage = false;

    [Header("Recovery Atributes")]
    [SerializeField] private float recoveryDelay;
    private float recoveryDelayTimer = 0f;
    [SerializeField][Range(0f, 1f)] private float recoveryPercent = 0.1f;

    [Header("One Shot Protection Atributes")]
    [SerializeField][Range(0f, 1f)] private float ospRange;
    [SerializeField][Range(0f, 1f)] private float healthRemainer;

    [Header("Dead Atributes")]
    public bool isDead = false;
    [SerializeField] private float bleedOutDuration;
    [HideInInspector] public float bleedOutTimer = 0f;
    private CorruptedNPC corruptedNPC;
    private bool isInBleedOut = false;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        TryGetComponent(out corruptedNPC);
    }

    private void OnEnable()
    {
        maximumHealthBar.maxValue = health;
        maximumHealthBar.value = health;
        hurtHealthBar.maxValue = health;
        hurtHealthBar.value = 0f;
        oneShotProtectionSlider.maxValue = health;
        oneShotProtectionSlider.value = health * ospRange;

        staminaBar.maxValue = stamina;
        staminaBar.value = stamina;

        bleedOutTimer = 0f;
        recoveryDelayTimer = 0f;
        isInBleedOut = false;
        isDead = false;

        // --- INÍCIO DA MUDANÇA 2 ---
        // A recuperação padrão é RÁPIDA (Stamina)
        currentRecoveryRate = staminaRecoveryRate;
        // --- FIM DA MUDANÇA 2 ---

        if (ownerType == ProjectileOwner.Player)
        {
            OnEnemyKilled += HandleRevenge;
        }
    }

    private void OnDisable()
    {
        if (ownerType == ProjectileOwner.Player)
        {
            OnEnemyKilled -= HandleRevenge;
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Esta é a sua lógica de "acumulação"
        staminaBar.maxValue = (maximumHealthBar.value / health) * stamina;

        hurtHealthBar.value = Mathf.Lerp(hurtHealthBar.value, health - maximumHealthBar.value, Time.deltaTime * 2f);
        oneShotProtectionSlider.value = maximumHealthBar.value * ospRange;

        if (maximumHealthBar.value <= 0f && !isInBleedOut)
        {
            if (corruptedNPC != null)
            {
                if (corruptedNPC.currentState == NPCState.Corrompido)
                {
                    corruptedNPC.EntrarEmNocaute();
                }
            }
            else
            {
                isInBleedOut = true;
                bleedOutSlider.gameObject.SetActive(true);
                bleedOutTimer = bleedOutDuration;
            }
        }

        if (isInBleedOut)
        {
            if (bleedOutTimer > 0f)
            {
                bleedOutTimer -= Time.deltaTime;
                bleedOutSlider.value = bleedOutTimer / bleedOutDuration;
            }
            else
            {
                bleedOutTimer = 0f;
                isDead = true;
                Kill();
            }
        }
        else
        {
            if (recoveryDelayTimer < recoveryDelay)
            {
                recoveryDelayTimer += Time.deltaTime;
            }
            else
            {
                // --- INÍCIO DA MUDANÇA 3 ---
                // Agora usa a taxa de recuperação correta (rápida ou lenta)
                if (staminaBar.value < staminaBar.maxValue)
                {
                    staminaBar.value += staminaBar.maxValue * recoveryPercent * currentRecoveryRate * Time.deltaTime;
                }
                else
                {
                    staminaBar.value = staminaBar.maxValue;
                }
                // --- FIM DA MUDANÇA 3 ---
            }
        }
    }

    private void HandleRevenge()
    {
        if (isInBleedOut && !isDead)
        {
            Debug.Log("REVENGE! Player recuperado do sangramento.");
            isInBleedOut = false;
            bleedOutTimer = 0f;
            bleedOutSlider.gameObject.SetActive(false);
            float healthToRecover = health * 0.5f;
            maximumHealthBar.value = healthToRecover;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        GameObject objectThatHitUs = collision.gameObject;
        EffectsLibrary effects = objectThatHitUs.GetComponent<EffectsLibrary>();
        Projectile projectile = objectThatHitUs.GetComponent<Projectile>();

        if (effects == null) return;

        if (projectile != null)
        {
            if (projectile.owner == this.ownerType && this.ownerType != ProjectileOwner.None)
            {
                return;
            }
        }
        ApplyEffect(effects.effects);
    }

    public bool CheckEffect(Effect[] effectToApply)
    {
        for (int i = 0; i < effectToApply.Length; i++)
        {
            switch (effectToApply[i].effectType)
            {
                case Effect.EffectType.physical:
                    if (effectToApply[i].power >= maximumHealthBar.value) return false;
                    break;
                case Effect.EffectType.magic:
                case Effect.EffectType.stamina:
                    if (effectToApply[i].power >= staminaBar.value) return false;
                    break;
            }
        }
        return true;
    }

    public bool ApplyEffect(Effect[] effectToApply)
    {
        if (!CheckEffect(effectToApply)) return false;

        for (int i = 0; i < effectToApply.Length; i++)
        {
            float power = effectToApply[i].power;

            switch (effectToApply[i].effectType)
            {
                case Effect.EffectType.physical:
                    justTookDamage = true;
                    recoveryDelayTimer = 0f;

                    // --- INÍCIO DA MUDANÇA 4 ---
                    // Penalidade: Se tomar dano, a recuperação de ação fica LENTA
                    currentRecoveryRate = manaRecoveryRate;
                    // --- FIM DA MUDANÇA 4 ---

                    if (power >= maximumHealthBar.value && maximumHealthBar.value >= (health * ospRange))
                    {
                        float remainingHealth = health * healthRemainer;
                        maximumHealthBar.value = remainingHealth;
                    }
                    else
                    {
                        maximumHealthBar.value -= power;
                    }
                    break;

                // --- INÍCIO DA MUDANÇA 5 ---
                case Effect.EffectType.magic:
                    recoveryDelayTimer = 0f;
                    staminaBar.value -= power;
                    // Define a recuperação como LENTA
                    currentRecoveryRate = manaRecoveryRate;
                    break;

                case Effect.EffectType.stamina:
                    recoveryDelayTimer = 0f;
                    staminaBar.value -= power;
                    // Define a recuperação como RÁPIDA
                    currentRecoveryRate = staminaRecoveryRate;
                    break;
                    // --- FIM DA MUDANÇA 5 ---
            }
        }
        return true;
    }

    public void RecoverHealth(float amount)
    {
        maximumHealthBar.value += amount;
        if (maximumHealthBar.value > health)
        {
            maximumHealthBar.value = health;
        }
    }

    private void Kill()
    {
        if (ownerType == ProjectileOwner.Player)
        {
            Debug.Log("JOGADOR MORREU!");
            if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
    public static void TriggerEnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }
}