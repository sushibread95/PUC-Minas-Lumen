using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

[RequireComponent(typeof(CorruptedNPC))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Attributes")]
    public int health = 50;
    [HideInInspector] public int currentHealth; // Público para ser ajustado no Load

    [Header("Animation")]
    public Animator animator;
    public string hurtTrigger = "Hurt";
    public string deathTrigger = "Die"; 
    public string fallenBool = "IsFallen"; 

    [Header("Fallen State")]
    [Tooltip("A % de vida para o inimigo cair (ex: 0.2 = 20% da vida)")]
    [SerializeField][Range(0.01f, 1f)] private float fallenThresholdPercent = 0.2f;
    [HideInInspector] public bool isFallen = false;
    [HideInInspector] public bool canBeKilledNormally = false; 

    [Header("UI (World Space)")]
    public Slider healthBarSlider; 

    [Header("Identity")]
    public ProjectileOwner ownerType = ProjectileOwner.Enemy;
    [HideInInspector] public bool isDead = false;

    private CorruptedNPC corruptedNPC;
    private LockOnTarget lockOnTarget;

    private void Awake()
    {
        corruptedNPC = GetComponent<CorruptedNPC>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        lockOnTarget = GetComponent<LockOnTarget>();
    }

    private void OnEnable()
    {
        // Reset padrão (será sobrescrito pelo CorruptedNPC.Start se houver Save)
        currentHealth = health;
        isDead = false;
        isFallen = false;
        canBeKilledNormally = false; 

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = health;
            healthBarSlider.value = health;
            healthBarSlider.gameObject.SetActive(true);
        }
        
        if (lockOnTarget != null) lockOnTarget.enabled = true;
    }

    private void LateUpdate()
    {
        // Se usar UIBillboard no slider, pode remover isso. 
        // Mantendo por segurança caso não tenha colocado o script lá ainda.
        if (healthBarSlider != null && Camera.main != null)
        {
            healthBarSlider.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return; 
        if (isFallen) return;

        GameObject objectThatHitUs = collision.gameObject;
        EffectsLibrary effects = objectThatHitUs.GetComponent<EffectsLibrary>();
        Projectile projectile = objectThatHitUs.GetComponent<Projectile>();

        if (effects == null) return;
        if (projectile != null && projectile.owner == this.ownerType) return; 

        ApplyEffect(effects.effects);
    }

    public bool ApplyEffect(Effect[] effectToApply)
    {
        if (isDead) return false;
        if (corruptedNPC != null && corruptedNPC.currentState == NPCState.Purificado) return false;
        if (isFallen && !canBeKilledNormally) return false;

        bool tookDamage = false; 

        for (int i = 0; i < effectToApply.Length; i++)
        {
            float power = effectToApply[i].power;

            switch (effectToApply[i].effectType)
            {
                case Effect.EffectType.physical:
                case Effect.EffectType.magic:

                    currentHealth -= (int)power;
                    tookDamage = true;

                    if (healthBarSlider != null) healthBarSlider.value = currentHealth;

                    if (currentHealth <= 0)
                    {
                        if (canBeKilledNormally)
                        {
                            Kill();
                        }
                        else if (!isFallen)
                        {
                            EnterFallenState();
                            currentHealth = 1; // Mantém 1 de vida para não morrer de vez
                        }
                    }
                    else if (currentHealth <= (health * fallenThresholdPercent) && !isFallen && !canBeKilledNormally)
                    {
                        EnterFallenState();
                    }
                    break;
            }
        }

        if (tookDamage && !isDead && !isFallen && animator != null)
        {
            animator.SetTrigger(hurtTrigger);
        }

        return true;
    }

    // --- MUDANÇA PRINCIPAL AQUI ---
private void EnterFallenState()
    {
        isFallen = true;
        if (animator != null) animator.SetBool(fallenBool, true);
        if (lockOnTarget != null) lockOnTarget.enabled = false;
        
        // NOVO: Aborta qualquer ataque imediatamente!
        EnemyAIController ai = GetComponent<EnemyAIController>();
        if (ai != null) ai.AbortCombat();

        // AVISA O GERENTE QUE CAIU!
        if (corruptedNPC != null)
        {
            corruptedNPC.EntrarEmNocaute();
        }
    }    
    // Método público para forçar o estado caído ao carregar o save
    public void ForceFallenStateOnLoad()
    {
        isFallen = true;
        currentHealth = 1;
        canBeKilledNormally = false;
        
        if (animator != null) animator.SetBool(fallenBool, true);
        if (lockOnTarget != null) lockOnTarget.enabled = false;
        if (healthBarSlider != null) healthBarSlider.value = currentHealth;
    }
    // -----------------------------

    public void RecoverFromFallen()
    {
        isFallen = false;
        canBeKilledNormally = true; 
        currentHealth = (int)(health * fallenThresholdPercent) + 1;

        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
            healthBarSlider.gameObject.SetActive(true);
        }

        if (animator != null) animator.SetBool(fallenBool, false);

        if (corruptedNPC != null)
        {
            // Avisa o gerente que levantou (para atualizar o save)
            corruptedNPC.RecuperarDeNocaute();
        }
        
        if (lockOnTarget != null) lockOnTarget.enabled = true;
    }

private void Kill()
    {
        if (isDead) return;
        isDead = true;

        HealthSystem.TriggerEnemyKilled();
        if (lockOnTarget != null) lockOnTarget.enabled = false;
        isFallen = false; 

        // NOVO: Corta a inteligência e a hitbox no milissegundo da morte (não espera 3s!)
        EnemyAIController ai = GetComponent<EnemyAIController>();
        if (ai != null) 
        {
            ai.AbortCombat();
            ai.enabled = false; // Desliga o Update da IA de vez
        }

        if (animator != null)
        {
            animator.SetBool(fallenBool, false); 
            animator.SetTrigger(deathTrigger);
        }
        
        StartCoroutine(DeathRoutine());
    }
    private IEnumerator DeathRoutine()
    {
        if (healthBarSlider != null) healthBarSlider.gameObject.SetActive(false);
        yield return new WaitForSeconds(3f);
        
        if (corruptedNPC != null) corruptedNPC.SerMorto(); 
        else Destroy(gameObject);
    }
}