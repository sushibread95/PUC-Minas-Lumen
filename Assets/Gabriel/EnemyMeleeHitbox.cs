using UnityEngine;
using System.Collections.Generic;

public class EnemyMeleeHitbox : MonoBehaviour
{
    [Header("Configuração")]
    public float damage = 15f;
    public float debugHitCooldown = 1.0f; // Só para não spammar o console
    private float lastHitTime = 0f;

    private Collider myCollider;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true; 
        myCollider.enabled = true; // --- FORÇA LIGADO SEMPRE ---
    }

    // Os métodos Enable/Disable ficaram vazios propositalmente para o Controller não interferir
    public void EnableHitbox(float dmg) { }
    public void DisableHitbox() { }

    void OnTriggerEnter(Collider other)
    {
        // 1. O Debug Definitivo: O que diabos essa mão está tocando?
        // Se isso não aparecer no console, a física do projeto está desligada para essas layers.
        // Trava de tempo para teste
        if (Time.time < lastHitTime + debugHitCooldown) return;

        if (other.CompareTag("Player")) 
        {
            Debug.Log("<color=green>ACHEI O PLAYER!</color>");
            
            HealthSystem targetHealth = other.GetComponent<HealthSystem>();
            if (targetHealth == null) targetHealth = other.GetComponentInParent<HealthSystem>();

            if (targetHealth != null)
            {
                lastHitTime = Time.time;
                Effect hitEffect = new Effect { effectType = Effect.EffectType.physical, power = this.damage };
                targetHealth.ApplyEffect(new Effect[] { hitEffect });
                Debug.Log($"<color=red>DANO APLICADO: {damage}</color>");
            }
            else
            {
                Debug.LogError("Player detectado, mas HealthSystem não encontrado!");
            }
        }
    }
}