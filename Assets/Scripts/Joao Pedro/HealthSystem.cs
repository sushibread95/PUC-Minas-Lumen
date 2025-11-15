using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Slider maximumHealthBar;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Slider hurtHealthBar;
    [SerializeField] private Slider oneShotProtectionSlider;
    [SerializeField] private Slider bleedOutSlider;
    [Header("Health Atributes")]
    public int health = 1;
    [Header("Identity")]
    [Tooltip("Defina quem é o 'dono' deste HealthSystem (Player ou Enemy)")]
    public ProjectileOwner ownerType = ProjectileOwner.None;
    [HideInInspector] public bool justTookDamage = false;
    [Header("Recovery Atributes")]
    [SerializeField] private float recoveryDelay;
    private float recoveryDelayTimer = 0f;
    [SerializeField] private float recoveryRate;
    [SerializeField][Range(0f, 1f)] private float recoveryPercent;
    [Header("One Shot Protection Atributes")]
    [SerializeField][Range(0f, 1f)] private float ospRange;
    [SerializeField][Range(0f, 1f)] private float healthRemainer;
    [Header("Dead Atributes")]
    public bool isDead = false;
    [SerializeField] private float bleedOutDuration;
    [HideInInspector] public float bleedOutTimer = 0f;
    private CorruptedNPC corruptedNPC;
    private void OnEnable()
    {
        bleedOutTimer = 0f;
        recoveryDelayTimer = 0f;
        maximumHealthBar.value = health;
        hurtHealthBar.value = 0f;
        TryGetComponent(out corruptedNPC);
        staminaBar.maxValue = maximumHealthBar.value;
        oneShotProtectionSlider.value = ospRange;
    }
    private void Update()
    {
        if (!isDead)
        {
            if (maximumHealthBar.value <= 0f)

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

                    bleedOutSlider.GetComponentInChildren<RawImage>().enabled = true;
                    if (bleedOutTimer < bleedOutDuration)
                    {
                        bleedOutTimer += Time.deltaTime;
                        bleedOutSlider.value = bleedOutSlider.maxValue - bleedOutTimer / bleedOutDuration;
                    }
                    else
                    {
                        bleedOutTimer = 0f;
                        isDead = true;
                        Kill();
                    }
                }
            }
            else bleedOutSlider.GetComponentInChildren<RawImage>().enabled = false;
            
            if (recoveryDelayTimer < recoveryDelay) recoveryDelayTimer += Time.deltaTime;
            else
            {
                if (maximumHealthBar.value < maximumHealthBar.maxValue - hurtHealthBar.value)
                {
                    maximumHealthBar.handleRect.GetComponent<RawImage>().enabled = true;
                    maximumHealthBar.value += health * recoveryPercent * recoveryRate * Time.deltaTime;
                    staminaBar.maxValue = maximumHealthBar.value;
                }
                else
                {
                    maximumHealthBar.handleRect.GetComponent<RawImage>().enabled = false;
                    maximumHealthBar.value = maximumHealthBar.maxValue - hurtHealthBar.value;
                }
                if (staminaBar.value < staminaBar.maxValue)
                {
                    staminaBar.handleRect.GetComponent<RawImage>().enabled = true;
                    staminaBar.value += staminaBar.maxValue * recoveryPercent * recoveryRate * 5 * Time.deltaTime;
                }
                else
                {
                    staminaBar.handleRect.GetComponent<RawImage>().enabled = false;
                    staminaBar.value = staminaBar.maxValue;
                }
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        // Pega o GameObject que nos acertou
        GameObject objectThatHitUs = collision.gameObject;

        // Tenta pegar os componentes desse objeto
        EffectsLibrary effects = objectThatHitUs.GetComponent<EffectsLibrary>();
        Projectile projectile = objectThatHitUs.GetComponent<Projectile>();

        // Se o objeto não tem "Efeitos" (dano), não faz nada.
        if (effects == null) return;

        // Checagem de Fogo Amigo (que implementamos antes)
        if (projectile != null)
        {
            // Se o dono do projétil é o MESMO dono deste HealthSystem, é fogo amigo.
            if (projectile.owner == this.ownerType)
            {
                return; // Para a execução, não aplica dano.
            }
        }

        // Se chegou aqui, é um ataque inimigo. Aplica o dano.
        ApplyEffect(effects.effects);
    }
    public bool CheckEffect(Effect[] effectToApply)
    {
        for (int i = 0; i < effectToApply.Length; i++)
        {
            switch (effectToApply[i].effectType)
            {
                case Effect.EffectType.physical:
                    recoveryDelayTimer = 0f;
                    if (effectToApply[i].power < health) return true;
                    else return false;
                case Effect.EffectType.magic:
                    if (effectToApply[i].power < maximumHealthBar.value) return true;
                    else return false;
                case Effect.EffectType.stamina:
                    break;
            }
        }
        return true;
        
    }
    public bool ApplyEffect(Effect[] effectToApply)
    {

        for (int i = 0; i < effectToApply.Length; i++)
        {
            switch (effectToApply[i].effectType)
            {
                case Effect.EffectType.physical:
                    justTookDamage = true;
                    recoveryDelayTimer = 0f;
                    if (effectToApply[i].power >= health && maximumHealthBar.value >= health * ospRange)
                    {
                        maximumHealthBar.value = health * healthRemainer;
                        staminaBar.maxValue = maximumHealthBar.value;
                        hurtHealthBar.value = health - (health * healthRemainer);
                    }
                    else
                    {
                        maximumHealthBar.value -= effectToApply[i].power;
                        staminaBar.maxValue = maximumHealthBar.value;
                        hurtHealthBar.value += effectToApply[i].power;
                    }
                    break;
                case Effect.EffectType.magic:
                    justTookDamage = true;
                    if (effectToApply[i].power < maximumHealthBar.value)
                    {
                        recoveryDelayTimer = 0f;
                        maximumHealthBar.value -= effectToApply[i].power;
                        staminaBar.maxValue = maximumHealthBar.value;
                    }
                    else return false;
                    break;
                case Effect.EffectType.stamina:
                    recoveryDelayTimer = 0f;
                    staminaBar.value -= effectToApply[i].power;
                    break;
            }
        }
        return true;
    }
    private void Kill()
    {
        if (ownerType == ProjectileOwner.Player)
        {
            Debug.Log("JOGADOR MORREU!");

            if (InputManager.Instance != null)
            {
                InputManager.Instance.SwitchToUIMap();
            }

            Time.timeScale = 1f;

            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            ObjectPoolingSystem.ReturnObjectToPool(this.gameObject);
        }
    }
}
