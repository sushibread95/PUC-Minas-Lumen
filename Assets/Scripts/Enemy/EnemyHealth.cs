using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CorruptedNPC))]
[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour
{
    #region Inspector - Health

    [Header("VIDA")]
    public int health = 50;
    [HideInInspector] public int currentHealth;

    [Header("IDENTIDADE")]
    public ProjectileOwner ownerType = ProjectileOwner.Enemy;
    [SerializeField] private EnemyIdentity identity;

    #endregion

    #region Inspector - Animation

    [Header("ANIMAÇÃO")]
    public Animator animator;
    public string hurtTrigger = "Hurt";
    public string deathTrigger = "Die";
    public string fallenBool = "IsFallen";

    #endregion

    #region Inspector - Fallen

    [Header("NOCAUTE")]
    [Tooltip("Porcentagem da vida para o inimigo cair. Ex: 0.2 = 20%")]
    [SerializeField][Range(0.01f, 1f)] private float fallenThresholdPercent = 0.2f;
    [HideInInspector] public bool isFallen = false;
    [HideInInspector] public bool canBeKilledNormally = false;

    #endregion

    #region Inspector - UI And Cleanup

    [Header("UI")]
    public Slider healthBarSlider;

    [Header("LIMPEZA DE MORTE")]
    [SerializeField] private EnemyDeathVisualCleanup deathVisualCleanup;
    [SerializeField] private float deathRoutineDelay = 3f;

    [Header("ESTADO RUNTIME")]
    [HideInInspector] public bool isDead = false;

    #endregion

    #region Runtime

    private CorruptedNPC corruptedNPC;
    private LockOnTarget lockOnTarget;
    private EnemyAIController aiController;
    private bool questDeathNotified;

    #endregion

    #region Unity Lifecycle

    private void Reset()
    {
        CacheComponents();
    }

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        ResetRuntimeState();
    }

    private void LateUpdate()
    {
        if (healthBarSlider != null && Camera.main != null)
            healthBarSlider.transform.rotation = Camera.main.transform.rotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead || isFallen)
            return;

        HandleHitObject(collision.gameObject);
    }

    #endregion

    #region Setup

    // Agrupa referências usadas por vida, IA, lock-on e limpeza visual.
    private void CacheComponents()
    {
        if (corruptedNPC == null) corruptedNPC = GetComponent<CorruptedNPC>();
        if (lockOnTarget == null) lockOnTarget = GetComponent<LockOnTarget>();
        if (aiController == null) aiController = GetComponent<EnemyAIController>();
        if (identity == null) identity = GetComponent<EnemyIdentity>();
        if (identity == null) identity = gameObject.AddComponent<EnemyIdentity>();
        if (deathVisualCleanup == null) deathVisualCleanup = GetComponent<EnemyDeathVisualCleanup>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    // Reinicia estado quando o inimigo/spawn é ativado.
    private void ResetRuntimeState()
    {
        currentHealth = health;
        isDead = false;
        isFallen = false;
        canBeKilledNormally = false;
        questDeathNotified = false;

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = health;
            healthBarSlider.value = health;
            healthBarSlider.gameObject.SetActive(true);
        }

        if (lockOnTarget != null)
            lockOnTarget.SetTargetable(true);
    }

    #endregion

    #region Damage

    private void HandleHitObject(GameObject objectThatHitUs)
    {
        EffectsLibrary effects = objectThatHitUs.GetComponent<EffectsLibrary>();
        Projectile projectile = objectThatHitUs.GetComponent<Projectile>();

        if (effects == null)
            return;

        if (projectile != null && projectile.owner == ownerType)
            return;

        ApplyEffect(effects.effects);
    }

    public bool ApplyEffect(Effect[] effectToApply)
    {
        if (effectToApply == null || effectToApply.Length == 0)
            return false;

        if (isDead)
            return false;

        if (corruptedNPC != null && corruptedNPC.currentState == NPCState.Purificado)
            return false;

        if (isFallen && !canBeKilledNormally)
            return false;

        bool tookDamage = false;

        for (int i = 0; i < effectToApply.Length; i++)
        {
            float power = effectToApply[i].power;

            switch (effectToApply[i].effectType)
            {
                case Effect.EffectType.physical:
                case Effect.EffectType.magic:
                    currentHealth -= Mathf.RoundToInt(power);
                    tookDamage = true;
                    UpdateHealthBar();
                    EvaluateHealthState();
                    break;
            }
        }

        if (tookDamage && !isDead && !isFallen && animator != null && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        return true;
    }

    private void EvaluateHealthState()
    {
        if (currentHealth <= 0)
        {
            if (canBeKilledNormally)
            {
                Kill();
            }
            else if (!isFallen)
            {
                EnterFallenState();
                currentHealth = 1;
                UpdateHealthBar();
            }

            return;
        }

        int fallenThreshold = Mathf.CeilToInt(health * fallenThresholdPercent);
        if (currentHealth <= fallenThreshold && !isFallen && !canBeKilledNormally)
            EnterFallenState();
    }

    #endregion

    #region Fallen

    private void EnterFallenState()
    {
        if (isDead || isFallen)
            return;

        isFallen = true;

        if (animator != null && !string.IsNullOrEmpty(fallenBool))
            animator.SetBool(fallenBool, true);

        if (lockOnTarget != null)
            lockOnTarget.SetTargetable(false);

        if (aiController != null)
            aiController.AbortCombat();

        if (corruptedNPC != null)
            corruptedNPC.EntrarEmNocaute();
    }

    public void ForceFallenStateOnLoad()
    {
        isFallen = true;
        currentHealth = 1;
        canBeKilledNormally = false;
        UpdateHealthBar();

        if (animator != null && !string.IsNullOrEmpty(fallenBool))
            animator.SetBool(fallenBool, true);

        if (lockOnTarget != null)
            lockOnTarget.SetTargetable(false);
    }

    public void RecoverFromFallen()
    {
        if (isDead)
            return;

        isFallen = false;
        canBeKilledNormally = true;
        currentHealth = Mathf.CeilToInt(health * fallenThresholdPercent) + 1;
        UpdateHealthBar();

        if (healthBarSlider != null)
            healthBarSlider.gameObject.SetActive(true);

        if (animator != null && !string.IsNullOrEmpty(fallenBool))
            animator.SetBool(fallenBool, false);

        if (corruptedNPC != null)
            corruptedNPC.RecuperarDeNocaute();

        if (lockOnTarget != null)
            lockOnTarget.SetTargetable(true);
    }

    #endregion

    #region Death

    private void Kill()
    {
        if (isDead)
            return;

        isDead = true;
        isFallen = false;

        NotifyDeathEventsOnce();

        if (lockOnTarget != null)
            lockOnTarget.SetTargetable(false);

        if (aiController != null)
        {
            aiController.AbortCombat();
            aiController.enabled = false;
        }

        if (animator != null)
        {
            if (!string.IsNullOrEmpty(fallenBool)) animator.SetBool(fallenBool, false);
            if (!string.IsNullOrEmpty(deathTrigger)) animator.SetTrigger(deathTrigger);
        }

        if (deathVisualCleanup != null)
            deathVisualCleanup.PlayDeathCleanup();

        StartCoroutine(DeathRoutine());
    }

    private void NotifyDeathEventsOnce()
    {
        if (questDeathNotified)
            return;

        questDeathNotified = true;
        // CORREÇÃO: HealthSystem.TriggerEnemyKilled() removido (evento sem
        // assinantes). O EnemyIdentity abaixo é o único notificador de quests.
        if (identity != null)
            identity.NotifyDeathForQuest();
    }

    private IEnumerator DeathRoutine()
    {
        if (healthBarSlider != null)
            healthBarSlider.gameObject.SetActive(false);

        yield return new WaitForSeconds(Mathf.Max(0f, deathRoutineDelay));

        if (corruptedNPC != null)
            corruptedNPC.SerMorto();
        else if (deathVisualCleanup == null)
            Destroy(gameObject);
    }

    #endregion

    #region UI

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
            healthBarSlider.value = currentHealth;
    }

    #endregion
}
