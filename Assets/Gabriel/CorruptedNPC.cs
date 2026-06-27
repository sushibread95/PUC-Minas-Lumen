using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI; 

public class CorruptedNPC : MonoBehaviour
{
    [Header("Identificação (MUITO IMPORTANTE)")]
    [Tooltip("Este ID deve ser ÚNICO para cada inimigo na cena.")]
    public string npcID;
    public string enemyTypeID; 

    [Header("Recompensas")]
    public float xpReward = 50f;

    [Header("UI de Decisão")]
    public GameObject decisionUIObject; 
    
    [Header("UI de Purificado")]
    public GameObject thankYouUIObject; 

    public float interactionRange = 5.0f;

    [Header("Estado")]
    public NPCState currentState = NPCState.Corrompido;
    public GameObject rotaParaAbrir;

    private EnemyHealth healthSystem;
    private Transform playerTransform;
    private PlayerInputActions input;
    private Animator animator; 
    private NavMeshAgent agent; 
    private Collider myCollider; 
    private EnemyAIController aiController;

    void Awake()
    {
        healthSystem = GetComponent<EnemyHealth>();
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        myCollider = GetComponent<Collider>();
        aiController = GetComponent<EnemyAIController>();
    }

    void Start()
    {
        // Garante UIs desligadas no início
        if (decisionUIObject != null) decisionUIObject.SetActive(false);
        if (thankYouUIObject != null) thankYouUIObject.SetActive(false);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerTransform = playerObj.transform;
        if (InputManager.Instance != null) input = InputManager.Instance.InputActions;

        // --- SISTEMA DE LOAD ---
        if (WorldStateManager.Instance != null)
        {
            if (WorldStateManager.Instance.npcWorldStates.TryGetValue(this.npcID, out NPCState estadoSalvo))
            {
                currentState = estadoSalvo;

                if (estadoSalvo == NPCState.Purificado)
                {
                    ApplyPurifiedState();
                    if (healthSystem && healthSystem.healthBarSlider) 
                        healthSystem.healthBarSlider.gameObject.SetActive(false);
                }
                else if (estadoSalvo == NPCState.Morto)
                {
                    TransformToCorpse(true); 
                }
                // NOVA LÓGICA: Carregar estado caído (Nocauteado)
                else if (estadoSalvo == NPCState.Nocauteado)
                {
                    ApplyFallenState();
                }
            }
        }
    }

    void Update()
    {
        if (currentState == NPCState.Morto) 
        {
            HideAllUI();
            return;
        }
        
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else return; 
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool isClose = dist <= interactionRange; 

        bool showDecision = false;
        bool showThankYou = false;

        if (currentState == NPCState.Purificado)
        {
            if (isClose) showThankYou = true;
        }
        else if (healthSystem != null && healthSystem.isFallen)
        {
            if (isClose) showDecision = true;
        }

        if (decisionUIObject != null && decisionUIObject.activeSelf != showDecision)
            decisionUIObject.SetActive(showDecision);
            
        if (thankYouUIObject != null && thankYouUIObject.activeSelf != showThankYou)
            thankYouUIObject.SetActive(showThankYou);

        if (showDecision && input != null)
        {
            if (input.Player.Purify.WasPressedThisFrame()) 
            {
                SerPurificado();
            }
            else if (input.Player.Kill.WasPressedThisFrame())
            {
                // CORREÇÃO: usa o caminho único de morte (SerMorto roteia pelo
                // EnemyHealth quando ele existe), igual ao NPCInteraction.
                SerMorto();
            }
        }
    }
    
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            if (decisionUIObject != null && decisionUIObject.activeSelf)
                decisionUIObject.transform.rotation = Camera.main.transform.rotation;
            
            if (thankYouUIObject != null && thankYouUIObject.activeSelf)
                thankYouUIObject.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void SerPurificado()
    {
        // CORREÇÃO (reentrância): vários scripts (CorruptedNPC, NPCInteraction,
        // FinishableNPC) podem chamar isto no mesmo frame. Sem esta guarda,
        // o XP de purificação era concedido em dobro.
        if (currentState == NPCState.Purificado || currentState == NPCState.Morto) return;

        currentState = NPCState.Purificado;
        HideAllUI();

        if (healthSystem != null && healthSystem.healthBarSlider != null)
            healthSystem.healthBarSlider.gameObject.SetActive(false);
        
        if (UIFeedbackManager.Instance != null) UIFeedbackManager.Instance.ShowNotification("Inimigo Purificado!", 2f);
        if (LevelingSystem.Instance != null) LevelingSystem.Instance.AddPurificationXP(xpReward);
        
        UpdateSaveState(NPCState.Purificado); // Salva
        
        if (rotaParaAbrir != null) rotaParaAbrir.SetActive(false);
        ApplyPurifiedState();

        // #1: notifica o ciclo de vida pelo ponto único (EnemyHealth).
        if (healthSystem != null) healthSystem.RaisePurified();
    }

    private void ApplyPurifiedState()
    {
        if (aiController != null) aiController.OnPurify();
    }

    public void SerMorto()
    {
        // CORREÇÃO (reentrância): impede dupla execução no mesmo frame.
        if (currentState == NPCState.Morto || currentState == NPCState.Purificado) return;

        // CORREÇÃO (caminho único de morte): se o inimigo ainda tem EnemyHealth
        // vivo, a morte é roteada por ele. Assim o EnemyIdentity dispara o evento
        // de quest UMA única vez e a animação/limpeza de morte rodam normalmente.
        // O EnemyHealth.DeathRoutine chama SerMorto() de novo no fim, e aí (com
        // isDead == true) caímos no bloco de "virar corpo" abaixo.
        if (healthSystem != null && !healthSystem.isDead)
        {
            healthSystem.canBeKilledNormally = true;
            healthSystem.ApplyEffect(new Effect[] { new Effect { effectType = Effect.EffectType.physical, power = 9999 } });
            return;
        }

        currentState = NPCState.Morto;
        HideAllUI();

        // CORREÇÃO (evento duplicado de quest): o TriggerEnemyDeath foi REMOVIDO
        // daqui. O evento de morte para quests agora é disparado apenas pelo
        // EnemyIdentity.NotifyDeathForQuest (chamado pelo EnemyHealth.Kill),
        // que tem trava de envio único. Antes, a mesma morte contava 2x.
        if (LevelingSystem.Instance != null) LevelingSystem.Instance.AddCombatXP(xpReward);

        UpdateSaveState(NPCState.Morto); // Salva

        TransformToCorpse(false);
    }

    // Chamado pelo EnemyHealth quando a vida zera
    public void EntrarEmNocaute()
    {
        if (currentState == NPCState.Purificado || currentState == NPCState.Morto) return;
        
        currentState = NPCState.Nocauteado;
        UpdateSaveState(NPCState.Nocauteado); // Salva imediatamente o estado caído!
    }

    // Chamado pelo EnemyHealth se ele recuperar vida (feature futura?)
    public void RecuperarDeNocaute()
    {
        currentState = NPCState.Corrompido;
        UpdateSaveState(NPCState.Corrompido);
    }

    // --- LÓGICA DE LOAD DO ESTADO CAÍDO ---
    private void ApplyFallenState()
    {
        if (healthSystem != null)
        {
            healthSystem.ForceFallenStateOnLoad();
        }
        // Se precisar parar IA ou algo assim, adicione aqui
    }

    private void UpdateSaveState(NPCState newState)
    {
        if (WorldStateManager.Instance != null) 
            WorldStateManager.Instance.SetNPCState(npcID, newState);
    }

    private void TransformToCorpse(bool instant)
    {
        if (myCollider) myCollider.enabled = false;
        if (agent) agent.enabled = false;
        if (aiController) aiController.enabled = false; 

        if (healthSystem && healthSystem.healthBarSlider) 
            healthSystem.healthBarSlider.gameObject.SetActive(false);

        if (animator)
        {
            if (instant) animator.Play("dead", 0, 1.0f); 
        }
        
        HideAllUI();
    }

    private void HideAllUI()
    {
        if (decisionUIObject != null) decisionUIObject.SetActive(false);
        if (thankYouUIObject != null) thankYouUIObject.SetActive(false);
    }
    
    void OnValidate()
    {
        if (string.IsNullOrEmpty(npcID)) npcID = System.Guid.NewGuid().ToString();
    }
}