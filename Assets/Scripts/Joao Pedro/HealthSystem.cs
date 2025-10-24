using UnityEngine;
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
    private void OnEnable()
    {
        bleedOutTimer = 0f;
        recoveryDelayTimer = 0f;
        maximumHealthBar.value = health;
        hurtHealthBar.value = 0f;
        staminaBar.maxValue = maximumHealthBar.value;
        oneShotProtectionSlider.value = ospRange;
    }
    private void Update()
    {
        if (!isDead)
        {
            if (maximumHealthBar.value == 0f)
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
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<EffectsLibrary>())
            ApplyEffect(other.GetComponent<EffectsLibrary>().effects);
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
        ObjectPoolingSystem.ReturnObjectToPool(this.gameObject);
    }
}
