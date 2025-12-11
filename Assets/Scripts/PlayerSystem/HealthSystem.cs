using UnityEngine;
using UnityEngine.SceneManagement; // Necessário para carregar o Menu
using UnityEngine.UI;
using System.Collections;
using System; 

public class HealthSystem : MonoBehaviour
{
    public static HealthSystem Instance { get; private set; }

    // Eventos Globais
    public static event Action OnEnemyKilled;
    public static event Action OnPlayerDied; 

    [Header("UI References")]
    private Slider healthBar;
    private Slider manaBar;

    [Header("Status")]
    public float currentHealth;
    public float currentMana;

    [Header("Identity")]
    private PlayerControllerSystem playerController;
    public ProjectileOwner ownerType = ProjectileOwner.None; // Geralmente 'Player' neste script

    [Header("Animation")]
    public Animator animator; 
    public string deathTrigger = "Die";
    public string hurtTrigger = "Hurt"; 

    [Header("Bleed Out / Morte")]
    public bool isDead = false;
    [SerializeField] private Slider bleedOutSlider;
    [SerializeField] private float bleedOutDuration = 5f;
    private float bleedOutTimer = 0f;
    private bool isInBleedOut = false;

    private CorruptedNPC corruptedNPC; // Referência opcional caso usado em NPC

    private void Awake()
    {
        playerController = GetComponent<PlayerControllerSystem>();
        TryGetComponent(out corruptedNPC);
        
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Lógica de Singleton apenas se for o Player
        if (playerController != null)
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }
    }

    private void Start()
    {
        // Se for Player, pega stats do PlayerStats. Se for NPC genérico, usa 100.
        if (playerController != null && PlayerStats.Instance != null)
        {
            currentHealth = PlayerStats.Instance.maxHealth;
            currentMana = PlayerStats.Instance.maxMana;
        }
        else if (corruptedNPC != null)
        {
            currentHealth = 100f;
        }

        // Conecta com o HUD
        if (HUDManager.Instance != null && playerController != null)
        {
            HUDManager.Instance.AssignSlidersTo(this);
        }
    }

    // --- DETECÇÃO DE DANO ---
    // 1. Detecta Magias e Armas (Is Trigger)
    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    // 2. Detecta Colisões Físicas
    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

    // Lógica unificada de recebimento de impacto
    private void HandleHit(GameObject attacker)
    {
        if (isDead) return;
        if (isInBleedOut) return;

        // Verifica se o Player está rolando (Invulnerável)
        if (playerController != null && playerController.IsInvulnerable()) return;

        // Tenta extrair o dano do objeto que bateu
        EffectsLibrary effects = attacker.GetComponent<EffectsLibrary>();
        Projectile projectile = attacker.GetComponent<Projectile>();

        if (effects == null) return; // Se não tem efeito, ignora

        // Fogo Amigo: Se for o próprio projétil do player, ignora
        if (projectile != null && projectile.owner == this.ownerType)
        {
            return; 
        }

        // Aplica o dano efetivamente
        ApplyEffect(effects.effects);
    }
    // -----------------------------------------------------------

    private void Update()
    {
        if (isDead) return;

        UpdateUIBars();

        // Checagem de Vida Zero
        if (currentHealth <= 0f && !isInBleedOut)
        {
            if (corruptedNPC != null)
            {
                if (corruptedNPC.currentState == NPCState.Corrompido) corruptedNPC.EntrarEmNocaute();
                else if (corruptedNPC.currentState != NPCState.Nocauteado) Destroy(gameObject);
            }
            else if (playerController != null) 
            {
                // Inicia Sangramento do Player
                isInBleedOut = true;
                if (bleedOutSlider) bleedOutSlider.gameObject.SetActive(true);
                bleedOutTimer = bleedOutDuration;
            }
        }

        // Lógica de Sangramento (Contagem regressiva para Game Over)
        if (isInBleedOut)
        {
            bleedOutTimer -= Time.deltaTime;
            if (bleedOutSlider) bleedOutSlider.value = bleedOutTimer / bleedOutDuration;

            if (bleedOutTimer <= 0f)
            {
                Kill();
            }
        }
    }

    public void RecoverHealth(float amount)
    {
        float maxH = (PlayerStats.Instance != null) ? PlayerStats.Instance.maxHealth : 100f;
        currentHealth += amount;
        if (currentHealth > maxH) currentHealth = maxH;
        
        // Se curar durante o sangramento, salva o player
        if (isInBleedOut && currentHealth > 0)
        {
            isInBleedOut = false;
            if (bleedOutSlider) bleedOutSlider.gameObject.SetActive(false);
        }
        
        UpdateUIBars();
    }

    public void RestoreMana(float amount)
    {
        float maxM = (PlayerStats.Instance != null) ? PlayerStats.Instance.maxMana : 50f;
        currentMana += amount;
        if (currentMana > maxM) currentMana = maxM;
        UpdateUIBars();
    }

    public bool ApplyEffect(Effect[] effectToApply)
    {
        // 1. Checa se tem Mana suficiente (para magias de custo)
        foreach (var effect in effectToApply)
        {
            if (effect.effectType == Effect.EffectType.magic)
            {
                if (currentMana < effect.power) return false;
            }
        }

        bool tookDamage = false; 

        // 2. Aplica os efeitos
        foreach (var effect in effectToApply)
        {
            float finalPower = effect.power;

            // Se for dano físico no Player, aplica defesa
            if (effect.effectType == Effect.EffectType.physical && playerController != null && PlayerStats.Instance != null)
            {
                finalPower = Mathf.Max(1f, finalPower - PlayerStats.Instance.defense);
            }

            switch (effect.effectType)
            {
                case Effect.EffectType.physical:
                    currentHealth -= finalPower;
                    tookDamage = true;
                    break;

                case Effect.EffectType.magic: 
                    currentMana -= finalPower;
                    break;
            }
        }

        // Toca animação de Hurt se tomou dano e não está morrendo
        if (tookDamage && !isDead && !isInBleedOut && animator != null)
        {
            animator.SetTrigger(hurtTrigger);
        }

        return true;
    }

    private void Kill()
    {
        if (isDead) return;
        isDead = true;

        if (playerController != null)
        {
            // Inicia a sequencia de Game Over Imediata
            StartCoroutine(PlayerDeathRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- AQUI ESTÁ A MUDANÇA (LoadScene direto) ---
    private IEnumerator PlayerDeathRoutine()
    {
        // 1. Trava Inputs (Segurança)
        if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
        
        // 2. Tenta tocar o início da animação de morte (Visual)
        if (animator != null)
        {
            animator.SetLayerWeight(1, 0f); 
            animator.SetTrigger(deathTrigger);
        }

        // 3. Avisa eventos (para quem estiver ouvindo, logs, analytics)
        OnPlayerDied?.Invoke();

        // 4. Espera um único frame para garantir que a engine processou a morte
        yield return null; 

        // 5. Destrói o Player Persistente
        // ISSO É IMPORTANTE: Para não voltar pro menu com um player "Zumbi" ativo.
        // Como o script está no Player, 'gameObject' refere-se ao próprio Player.
        Destroy(gameObject);

        // 6. Carrega o Menu Principal imediatamente
        SceneManager.LoadScene("MainMenu");
    }
    // ----------------------------------------------

    // Auxiliares de UI
    public void SetSliders(Slider hp, Slider mana, Slider bleed)
    {
        this.healthBar = hp;
        this.manaBar = mana;
        this.bleedOutSlider = bleed;
        UpdateUIBars();
    }

    public void UpdateMaxStats(float newMaxHealth, float newMaxMana)
    {
        currentHealth = newMaxHealth;
        currentMana = newMaxMana;
        UpdateUIBars();
    }

    private void UpdateUIBars()
    {
        if (playerController == null) return;

        float maxH = (PlayerStats.Instance != null) ? PlayerStats.Instance.maxHealth : 100f;
        float maxM = (PlayerStats.Instance != null) ? PlayerStats.Instance.maxMana : 50f;

        if (healthBar) { healthBar.maxValue = maxH; healthBar.value = currentHealth; }
        if (manaBar) { manaBar.maxValue = maxM; manaBar.value = currentMana; }
    }

    public bool CheckEffect(Effect[] effectToApply)
    {
        if (effectToApply == null) return true;
        for (int i = 0; i < effectToApply.Length; i++)
        {
            if (effectToApply[i].effectType == Effect.EffectType.magic)
            {
                if (currentMana < effectToApply[i].power) return false; 
            }
        }
        return true; 
    }

    public static void TriggerEnemyKilled()
    {
        OnEnemyKilled?.Invoke();
    }
}