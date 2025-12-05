using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Necessário para Coroutines

[RequireComponent(typeof(CorruptedNPC))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Attributes")]
    public int health = 50;
    private int currentHealth;

    [Header("Animation")]
    public Animator animator;
    public string hurtTrigger = "Hurt";
    public string deathTrigger = "Die"; // Certifique-se que o Trigger no Animator é "Die"
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

    private void Awake()
    {
        corruptedNPC = GetComponent<CorruptedNPC>();
        // Tenta pegar o animator automaticamente se esquecer de arrastar
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
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
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return; 
        if (isFallen) return;

        GameObject objectThatHitUs = collision.gameObject;
        EffectsLibrary effects = objectThatHitUs.GetComponent<EffectsLibrary>();
        Projectile projectile = objectThatHitUs.GetComponent<Projectile>();

        if (effects == null) return;
        if (projectile != null && projectile.owner == this.ownerType)
        {
            return; 
        }

        ApplyEffect(effects.effects);
    }

    public bool ApplyEffect(Effect[] effectToApply)
    {
        if (isDead) return false;
        // Se estiver caído, só aceita dano se a flag de morte estiver liberada (pelo golpe final)
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

                    if (healthBarSlider != null)
                    {
                        healthBarSlider.value = currentHealth;
                    }

                    // Se a vida zerar...
                    if (currentHealth <= 0)
                    {
                        // Se já caiu e recebeu o golpe final (canBeKilledNormally)...
                        if (canBeKilledNormally)
                        {
                            Kill();
                        }
                        // Se ainda não caiu, mas a vida zerou por dano massivo...
                        // (Opcional: você pode forçar ele a cair ou morrer direto. 
                        // Aqui mantivemos a lógica de cair se chegar no threshold, 
                        // mas se for 0 ele deve cair imediatamente para ser finalizado).
                        else if (!isFallen)
                        {
                            EnterFallenState();
                            // Deixa com 1 de vida para não bugar a lógica de morte depois
                            currentHealth = 1; 
                        }
                    }
                    // Se a vida só baixou do limite...
                    else if (currentHealth <= (health * fallenThresholdPercent) && !isFallen && !canBeKilledNormally)
                    {
                        EnterFallenState();
                    }
                    break;

                case Effect.EffectType.stamina:
                    break;
            }
        }

        // Toca animação de dano (apenas se não estiver morto ou caído)
        if (tookDamage && !isDead && !isFallen && animator != null)
        {
            animator.SetTrigger(hurtTrigger);
        }

        return true;
    }

    private void EnterFallenState()
    {
        isFallen = true;
        if (animator != null) animator.SetBool(fallenBool, true);
    }

    public void RecoverFromFallen()
    {
        isFallen = false;
        canBeKilledNormally = true; // Agora ele pode morrer em combate normal

        currentHealth = (int)(health * fallenThresholdPercent) + 1;

        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
            healthBarSlider.gameObject.SetActive(true);
        }

        if (animator != null) animator.SetBool(fallenBool, false);

        if (corruptedNPC != null)
        {
            corruptedNPC.currentState = NPCState.Corrompido;
        }
    }

    private void Kill()
    {
        if (isDead) return;
        isDead = true;

        HealthSystem.TriggerEnemyKilled();
        
        // --- ADIÇÃO CRÍTICA: Desliga o ímã do "Fallen" ---
        isFallen = false; 
        if (animator != null)
        {
            animator.SetBool(fallenBool, false); // <--- ISSO IMPEDE DE VOLTAR
            animator.SetTrigger(deathTrigger);
        }
        // --------------------------------------------------
        
        StartCoroutine(DeathRoutine());
    }
    private IEnumerator DeathRoutine()
    {
        // Esconde a barra de vida imediatamente
        if (healthBarSlider != null) healthBarSlider.gameObject.SetActive(false);

        // Toca animação de morte
        if (animator != null)
        {
            animator.SetTrigger(deathTrigger);
        }

        // Espera a animação terminar (ajuste o tempo conforme sua animação)
        yield return new WaitForSeconds(3f);

        // Chama a lógica final para transformar em cadáver
        if (corruptedNPC != null)
        {
            corruptedNPC.SerMorto(); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
}