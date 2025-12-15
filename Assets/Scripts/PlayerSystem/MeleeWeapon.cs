using UnityEngine;
using System.Collections.Generic;

public class MeleeWeapon : MonoBehaviour
{
    [Header("Configuração")]
    public ProjectileOwner ownerType = ProjectileOwner.Player;
    public LayerMask targetLayers; 

    [Header("Detecção de Colisão (Raycast)")]
    public Transform basePoint; 
    public Transform tipPoint; 
    public int raycastResolution = 5; 

    private float currentDamage;
    private List<GameObject> hitTargets = new List<GameObject>(); 
    
    // Agora separamos: "Rastreando" vs "Causando Dano"
    private bool isDealingDamage = false; 

    private Vector3[] previousPoints;
    private bool initialized = false;

    void Awake()
    {
        // Garante que a física normal não atrapalhe
        Collider c = GetComponent<Collider>();
        if(c) c.enabled = false;
        
        // Auto-configuração de falha
        if (basePoint == null && c != null) { /* Lógica de auto-setup (opcional) */ }
    }

    void Start()
    {
        // Inicializa o array uma única vez
        previousPoints = new Vector3[raycastResolution];
        if (basePoint && tipPoint)
        {
            for (int i = 0; i < raycastResolution; i++)
            {
                previousPoints[i] = Vector3.Lerp(basePoint.position, tipPoint.position, (float)i / (raycastResolution - 1));
            }
        }
        initialized = true;
    }

    public void Initialize(float damageStats, ProjectileOwner owner)
    {
        this.currentDamage = damageStats;
        this.ownerType = owner;
    }

    // Chamado quando o ataque começa (dano valendo!)
    public void EnableHitbox()
    {
        hitTargets.Clear();
        isDealingDamage = true;
        // NÃO resetamos mais os previousPoints aqui. Usamos o rastro contínuo do LateUpdate.
    }

    // Chamado quando o ataque termina
    public void DisableHitbox()
    {
        isDealingDamage = false;
    }

    // O Segredo: Rastreia SEMPRE, Dano só quando isDealingDamage é true
    void LateUpdate()
    {
        if (!initialized || basePoint == null || tipPoint == null) return;

        // Garante redimensionamento se mudar no inspector
        if (previousPoints.Length != raycastResolution) previousPoints = new Vector3[raycastResolution];

        for (int i = 0; i < raycastResolution; i++)
        {
            float t = (raycastResolution > 1) ? (float)i / (raycastResolution - 1) : 0f;
            Vector3 currentPointPos = Vector3.Lerp(basePoint.position, tipPoint.position, t);
            
            // Pega onde estava no último frame
            Vector3 previousPointPos = previousPoints[i];
            
            // Só processa colisão se a espada se moveu
            Vector3 direction = currentPointPos - previousPointPos;
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                // A MÁGICA: Só checa colisão e dá dano se isDealingDamage for TRUE
                if (isDealingDamage)
                {
                    RaycastHit[] hits = Physics.RaycastAll(previousPointPos, direction.normalized, distance, targetLayers);
                    foreach (RaycastHit hit in hits)
                    {
                        CheckDamage(hit.collider);
                    }
                     // Debug visual: Vermelho = Matando, Branco = Apenas Rastreando
                    Debug.DrawLine(previousPointPos, currentPointPos, Color.red, 0.5f);
                }
                else
                {
                    // Debug para você ver que ele está rastreando sempre
                    Debug.DrawLine(previousPointPos, currentPointPos, Color.white, 0.1f);
                }
            }

            // Atualiza o ponto anterior para o próximo frame (ISSO RODA SEMPRE)
            previousPoints[i] = currentPointPos;
        }
    }

    private void CheckDamage(Collider other)
    {
        if (hitTargets.Contains(other.gameObject)) return;

        // Lógica de Player
        if (ownerType == ProjectileOwner.Player)
        {
            EnemyHealth enemyHp = other.GetComponentInParent<EnemyHealth>();
            if (enemyHp != null)
            {
                // Evita bater em si mesmo se a layer estiver errada
                if (enemyHp.gameObject == transform.root.gameObject) return;

                Effect dmgEffect = new Effect { effectType = Effect.EffectType.physical, power = currentDamage };
                if (enemyHp.ApplyEffect(new Effect[] { dmgEffect }))
                {
                    hitTargets.Add(other.gameObject);
                    if (HealthSystem.Instance != null) HealthSystem.Instance.RestoreMana(5f);
                    Debug.Log($"<color=yellow>HIT CONFIRMADO:</color> {enemyHp.name}");
                }
            }
        }
        // Lógica de Inimigo
        else if (ownerType == ProjectileOwner.Enemy)
        {
            HealthSystem playerHp = other.GetComponentInParent<HealthSystem>();
            if (playerHp != null)
            {
                Effect dmgEffect = new Effect { effectType = Effect.EffectType.physical, power = currentDamage };
                if (playerHp.ApplyEffect(new Effect[] { dmgEffect }))
                {
                    hitTargets.Add(other.gameObject);
                }
            }
        }
    }
}