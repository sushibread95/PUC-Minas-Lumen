using UnityEngine;
using System.Collections.Generic;

public class EnemyMeleeHitbox : MonoBehaviour
{
    [Header("Configuração")]
    public float damage = 15f;
    public float debugHitCooldown = 1.0f; 
    private float lastHitTime = 0f;

    private Collider myCollider;
    
    // --- ADIÇÃO: Referências ao 'cérebro' do inimigo ---
    private EnemyAIController myAI;
    private EnemyHealth myHealth;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true; 
        
        // 1. CORREÇÃO CRÍTICA: O punho começa DESLIGADO. Fim da morte por esbarrão!
        myCollider.enabled = false; 

        // Pega as referências do corpo principal
        myAI = GetComponentInParent<EnemyAIController>();
        myHealth = GetComponentInParent<EnemyHealth>();
    }

    // 2. CORREÇÃO: Agora o script obedece a animação e liga/desliga a mão
    public void EnableHitbox(float dmg) 
    { 
        damage = dmg;
        if (myCollider) myCollider.enabled = true; 
    }
    
    public void DisableHitbox() 
    { 
        if (myCollider) myCollider.enabled = false; 
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time < lastHitTime + debugHitCooldown) return;

        // 3. A TRAVA DEFINITIVA: Se o inimigo tá purificado, morto ou caído, a mão não dá dano!
        if (myAI != null && myAI.isPurified) return;
        if (myHealth != null && (myHealth.isDead || myHealth.isFallen)) return;

        if (other.CompareTag("Player")) 
        {
            HealthSystem targetHealth = other.GetComponent<HealthSystem>();
            if (targetHealth == null) targetHealth = other.GetComponentInParent<HealthSystem>();

            if (targetHealth != null)
            {
                // Se passou pelas travas, é um soco válido. Aplica o dano!
                lastHitTime = Time.time;
                Effect hitEffect = new Effect { effectType = Effect.EffectType.physical, power = this.damage };
                targetHealth.ApplyEffect(new Effect[] { hitEffect });
                
                Debug.Log($"<color=red>DANO APLICADO NO PLAYER: {damage}</color>");
            }
        }
    }
}