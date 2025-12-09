using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class MeleeWeapon : MonoBehaviour
{
    [Header("Configuração")]
    public ProjectileOwner ownerType = ProjectileOwner.Player;
    public LayerMask targetLayers; // Configure para pegar "Enemy" (se for player) ou "Player" (se for inimigo)

    private Collider hitBox;
    private float currentDamage;
    private List<GameObject> hitTargets = new List<GameObject>(); // Para não bater 2x no mesmo inimigo no mesmo swing
    private bool isAttacking = false;

    void Awake()
    {
        hitBox = GetComponent<Collider>();
        hitBox.isTrigger = true;
        hitBox.enabled = false; // Começa desligado
    }

    // Chamado pelo PlayerController quando equipa a arma
    public void Initialize(float damageStats, ProjectileOwner owner)
    {
        // O dano final é: Dano da Arma + Dano do Player (Força)
        this.currentDamage = damageStats;
        this.ownerType = owner;
    }

    // Liga a área de dano (Chamado na animação)
    public void EnableHitbox()
    {
        hitTargets.Clear(); // Limpa a lista de quem já apanhou neste golpe
        hitBox.enabled = true;
        isAttacking = true;
    }

    // Desliga a área de dano
    public void DisableHitbox()
    {
        hitBox.enabled = false;
        isAttacking = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isAttacking) return;

        // Verifica se está na layer certa (usando bitwise operation ou CompareTag)
        if (((1 << other.gameObject.layer) & targetLayers) != 0)
        {
            // Evita dano duplo no mesmo frame/ataque
            if (hitTargets.Contains(other.gameObject)) return;

            // Tenta causar dano
            // 1. Procura HealthSystem (Player)
            HealthSystem playerHp = other.GetComponent<HealthSystem>();
            if (playerHp != null && ownerType == ProjectileOwner.Enemy)
            {
                Effect dmgEffect = new Effect { effectType = Effect.EffectType.physical, power = currentDamage };
                if (playerHp.ApplyEffect(new Effect[] { dmgEffect }))
                {
                    hitTargets.Add(other.gameObject);
                    // Tocar som de hit aqui
                    Debug.Log("Acertou o Player!");
                }
            }

            // 2. Procura EnemyHealth (Inimigo)
            EnemyHealth enemyHp = other.GetComponent<EnemyHealth>();
            if (enemyHp != null && ownerType == ProjectileOwner.Player)
            {
                Effect dmgEffect = new Effect { effectType = Effect.EffectType.physical, power = currentDamage };
                if (enemyHp.ApplyEffect(new Effect[] { dmgEffect }))
                {
                    hitTargets.Add(other.gameObject);

                    // RECUPERA MANA AO ACERTAR (Sua mecânica!)
                    if (HealthSystem.Instance != null)
                    {
                        HealthSystem.Instance.RestoreMana(5f); // Valor fixo ou variável
                    }

                    Debug.Log("Acertou o Inimigo!");
                }
            }
        }
    }
}