using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CorruptedNPC))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Attributes")]
    public int health = 50;
    private int currentHealth;

    [Header("Fallen State")]
    [Tooltip("A % de vida para o inimigo cair (ex: 0.2 = 20% da vida)")]
    [SerializeField][Range(0.01f, 1f)] private float fallenThresholdPercent = 0.2f;
    [HideInInspector] public bool isFallen = false;

    // --- INÍCIO DAS MUDANÇAS ---
    [HideInInspector] public bool canBeKilledNormally = false; // Flag para "morrer de vez"
    // --- FIM DAS MUDANÇAS ---

    [Header("UI (World Space)")]
    [Tooltip("Arraste o Slider da barra de vida que fica em cima da cabeça do inimigo aqui")]
    public Slider healthBarSlider; // Deixei público para o FallenState acessar

    [Header("Identity")]
    public ProjectileOwner ownerType = ProjectileOwner.Enemy;
    [HideInInspector] public bool isDead = false;

    private CorruptedNPC corruptedNPC;

    private void Awake()
    {
        corruptedNPC = GetComponent<CorruptedNPC>();
    }

    private void OnEnable()
    {
        currentHealth = health;
        isDead = false;
        isFallen = false;
        canBeKilledNormally = false; // --- LINHA ADICIONADA ---

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = health;
            healthBarSlider.value = health;
            healthBarSlider.gameObject.SetActive(true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return; // Se já está morto, ignora

        // Se está caído, não pode tomar dano
        if (isFallen) return;

        GameObject objectThatHitUs = collision.gameObject;
        EffectsLibrary effects = objectThatHitUs.GetComponent<EffectsLibrary>();
        Projectile projectile = objectThatHitUs.GetComponent<Projectile>();

        if (effects == null) return;
        if (projectile != null && projectile.owner == this.ownerType)
        {
            return; // Fogo amigo
        }

        ApplyEffect(effects.effects);
    }

    public bool ApplyEffect(Effect[] effectToApply)
    {
        if (isDead || isFallen) return false;

        for (int i = 0; i < effectToApply.Length; i++)
        {
            float power = effectToApply[i].power;

            switch (effectToApply[i].effectType)
            {
                case Effect.EffectType.physical:
                case Effect.EffectType.magic:

                    currentHealth -= (int)power;

                    if (healthBarSlider != null)
                    {
                        healthBarSlider.value = currentHealth;
                    }

                    // --- INÍCIO DAS MUDANÇAS ---

                    // Se a vida zerar E ele já se levantou uma vez...
                    if (currentHealth <= 0 && canBeKilledNormally)
                    {
                        // ...MORRE DE VEZ.
                        Kill();
                    }
                    // Se a vida baixar do limite E ele ainda NÃO caiu...
                    else if (currentHealth <= (health * fallenThresholdPercent) && !isFallen && !canBeKilledNormally)
                    {
                        // ...CAI (para o player decidir).
                        isFallen = true;
                    }
                    // --- FIM DAS MUDANÇAS ---
                    break;

                case Effect.EffectType.stamina:
                    break;
            }
        }
        return true;
    }

    // --- INÍCIO DAS MUDANÇAS ---
    // Chamado pelo FallenState quando o timer acaba
    public void RecoverFromFallen()
    {
        isFallen = false;
        canBeKilledNormally = true; // Agora ele pode morrer de vez

        // Cura o inimigo um pouco acima do limite de "cair"
        currentHealth = (int)(health * fallenThresholdPercent) + 1;

        // Mostra a barra de vida de novo
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
            healthBarSlider.gameObject.SetActive(true);
        }

        // Reseta o estado do NPC
        if (corruptedNPC != null)
        {
            corruptedNPC.currentState = NPCState.Corrompido;
        }
    }
    // --- FIM DAS MUDANÇAS ---

    private void Kill()
    {
        if (isDead) return;
        isDead = true;

        HealthSystem.TriggerEnemyKilled();

        // --- INÍCIO DAS MUDANÇAS ---
        // Se a função Kill() foi chamada, é porque ele deve MORRER,
        // não apenas ser nocauteado.
        if (corruptedNPC != null)
        {
            corruptedNPC.SerMorto(); // Chama a morte final
        }
        // --- FIM DAS MUDANÇAS ---

        if (healthBarSlider != null)
        {
            healthBarSlider.gameObject.SetActive(false);
        }
    }
}