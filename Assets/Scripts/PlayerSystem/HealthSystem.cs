using UnityEngine;
using UnityEngine.SceneManagement; // NECESSÁRIO para carregar cenas
using UnityEngine.UI;
using System.Collections;
using System; 

public class HealthSystem : MonoBehaviour, IDamageable
{
    public static HealthSystem Instance { get; private set; }

    // Eventos Globais
    // CORREÇÃO: o evento 'OnEnemyKilled' foi removido — era disparado mas não
    // tinha NENHUM assinante no projeto. O fluxo real de morte para quests
    // passa por GameEvents.OnEnemyDeath (via EnemyIdentity).
    public static event Action OnPlayerDied;

    [Header("UI References")]
    private Image healthBar;
    private Image manaBar;

    

    [Header("Status")]
    public float currentHealth;
    public float currentMana;

    [Header("Identity")]
    private PlayerControllerSystem playerController;
    public ProjectileOwner ownerType = ProjectileOwner.None; 

    [Header("Animation")]
    public Animator animator; 
    public string deathTrigger = "Die";
    public string hurtTrigger = "Hurt"; 

    [Header("Death Settings (NOVO)")]
    [Tooltip("Tempo de espera (animação) antes de reiniciar a cena")]
    public float deathDelay = 4.0f; 
    [Tooltip("Nome da cena de Game Over. Deixe vazio para apenas REINICIAR a fase atual.")]
    public string sceneAfterDeath = ""; 

    [Header("Bleed Out / Morte")]
    public bool isDead = false;
    [SerializeField] private Image bleedOutBar;
    [SerializeField] private float bleedOutDuration = 5f;
    private float bleedOutTimer = 0f;
    private bool isInBleedOut = false;

    private CorruptedNPC corruptedNPC; 

    private void Awake()
    {
        playerController = GetComponent<PlayerControllerSystem>();
        TryGetComponent(out corruptedNPC);
        
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (playerController != null)
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;

            // CORREÇÃO (autodano): se for o Player e ninguém configurou o
            // ownerType no Inspector, define automaticamente como Player.
            // Sem isso, a checagem 'projectile.owner == ownerType' no HandleHit
            // não filtrava os projéteis do próprio jogador.
            if (ownerType == ProjectileOwner.None)
                ownerType = ProjectileOwner.Player;
        }
    }

    private void Start()
    {
        if (playerController != null && PlayerStats.Instance != null)
        {
            currentHealth = PlayerStats.Instance.maxHealth;
            currentMana = PlayerStats.Instance.maxMana;
        }
        else if (corruptedNPC != null)
        {
            currentHealth = 100f;
        }

        if (HUDManager.Instance != null && playerController != null)
        {
            HUDManager.Instance.AssignBarsTo(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject);
    }

        public void HandleHit(GameObject attacker)
    {
        // 1. Trava do Player: Se você já morreu, não faz sentido processar mais dano.
        if (isDead || isInBleedOut) return;
        
        // 2. Trava de Esquiva (Invulnerabilidade)
        if (playerController != null && playerController.IsInvulnerable()) return;

        EnemyAIController enemyAI = attacker.GetComponentInParent<EnemyAIController>();
        EnemyHealth enemyHealth = attacker.GetComponentInParent<EnemyHealth>();

        if (enemyAI != null && enemyAI.isPurified) return; 

        if (enemyHealth != null)
        {
            if (enemyHealth.isDead || enemyHealth.isFallen) return;
        }

        EffectsLibrary effects = attacker.GetComponent<EffectsLibrary>();
        Projectile projectile = attacker.GetComponent<Projectile>();

        if (effects == null) return; 
        if (projectile != null && projectile.owner == this.ownerType) return; 

        ApplyEffect(effects.effects);
    }
    
    
    private void Update()
    {
        if (isDead) return;

        UpdateUIBars();

        if (currentHealth <= 0f && !isInBleedOut)
        {
            if (corruptedNPC != null)
            {
                if (corruptedNPC.currentState == NPCState.Corrompido) corruptedNPC.EntrarEmNocaute();
                else if (corruptedNPC.currentState != NPCState.Nocauteado) Destroy(gameObject);
            }
            else if (playerController != null) 
            {
                isInBleedOut = true;
                if (bleedOutBar)
                    bleedOutBar.gameObject.SetActive(true);
                bleedOutTimer = bleedOutDuration;
            }
        }

        if (isInBleedOut)
        {
            bleedOutTimer -= Time.deltaTime;
            if (bleedOutBar)
                bleedOutBar.fillAmount = bleedOutTimer / bleedOutDuration;

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
        
        if (isInBleedOut && currentHealth > 0)
        {
            isInBleedOut = false;
            if (bleedOutBar)
                bleedOutBar.gameObject.SetActive(false);
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
        foreach (var effect in effectToApply)
        {
            if (effect.effectType == Effect.EffectType.magic)
            {
                if (currentMana < effect.power) return false;
            }
        }

        bool tookDamage = false; 

        foreach (var effect in effectToApply)
        {
            float finalPower = effect.power;

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
            StartCoroutine(PlayerDeathRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- AQUI ESTÁ A LÓGICA DE RELOAD AUTOMÁTICO ---
    private IEnumerator PlayerDeathRoutine()
    {
        // 1. Toca Animação
        if (animator != null)
        {
            animator.SetLayerWeight(1, 0f); 
            animator.SetTrigger(deathTrigger);
        }

        // 2. Avisa outros scripts (ex: inimigos param de atacar)
        OnPlayerDied?.Invoke();

        // 3. Espera o player "curtir" a animação de morte
        yield return new WaitForSeconds(deathDelay);

        // 4. DESTRÓI O PLAYER PERSISTENTE
        // Isso é vital! Se não destruir, ao recarregar a cena haverá dois players
        // ou o player nascerá morto.
        if (PlayerPersistent.Instance != null)
        {
            Destroy(PlayerPersistent.Instance.gameObject);
        }

        string targetScene = string.IsNullOrEmpty(sceneAfterDeath) ? SceneManager.GetActiveScene().name : sceneAfterDeath;
        
        if (TransitionManager.Instance != null)
        {
            // O TransitionManager cuida de tela de loading, resetar o tempo e travar o mouse para a nova tentativa!
            TransitionManager.Instance.TransitionToScene(targetScene, "", true);
        }
        else
        {
            // Fallback de segurança apenas para testes
            SceneManager.LoadSceneAsync(targetScene);
        }
        
        }
    // ----------------------------------------------

    public void SetBars(Image hp, Image mana, Image bleed)
    {
        healthBar = hp;
        manaBar = mana;
        bleedOutBar = bleed;
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

        if (healthBar)
            healthBar.fillAmount = currentHealth / maxH;

        if (manaBar)
            manaBar.fillAmount = currentMana / maxM;
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

    // CORREÇÃO: TriggerEnemyKilled() removido junto com o evento OnEnemyKilled
    // (não havia assinantes). Mortes para quest usam GameEvents.TriggerEnemyDeath.
}