using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI; 

public class CorruptedNPC : MonoBehaviour
{
    [Header("Identificação Única (Save System)")]
    public string npcID; // ÚNICO para cada boneco (não mexa)

    [Header("Identificação de Quest")]
    [Tooltip("Nome do TIPO do inimigo. Ex: 'Goblin', 'Lobo'. Deve ser IGUAL ao Target ID na Quest.")]
    public string enemyTypeID; // --- NOVO CAMPO PARA A QUEST ---

    [Header("Recompensas")]
    public float xpReward = 50f;

    [Header("UI de Decisão")]
    [Tooltip("Arraste o GameObject 'DecisionCanvas' aqui.")]
    public GameObject decisionUIObject; 
    public float interactionRange = 3.0f;
    public float fadeSpeed = 5.0f;

    [Header("Estado")]
    public NPCState currentState = NPCState.Corrompido;
    public GameObject rotaParaAbrir;

    private CanvasGroup uiCanvasGroup;
    private EnemyHealth healthSystem;
    private Transform playerTransform;
    private PlayerInputActions input;
    private Animator animator; 
    private NavMeshAgent agent; 
    private Collider myCollider; 

    void Awake()
    {
        healthSystem = GetComponent<EnemyHealth>();
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        myCollider = GetComponent<Collider>();
    }

    void Start()
    {
        // Setup da UI
        if (decisionUIObject != null)
        {
            uiCanvasGroup = decisionUIObject.GetComponent<CanvasGroup>();
            if (uiCanvasGroup == null) uiCanvasGroup = decisionUIObject.AddComponent<CanvasGroup>();
            uiCanvasGroup.alpha = 0f;
            decisionUIObject.SetActive(true);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerTransform = playerObj.transform;
        if (InputManager.Instance != null) input = InputManager.Instance.InputActions;

        // Carregar Save
        if (WorldStateManager.Instance != null)
        {
            if (WorldStateManager.Instance.npcWorldStates.TryGetValue(this.npcID, out NPCState estadoSalvo))
            {
                currentState = estadoSalvo;
                if (estadoSalvo == NPCState.Purificado)
                {
                    if (rotaParaAbrir != null) rotaParaAbrir.SetActive(false);
                    gameObject.SetActive(false);
                }
                else if (estadoSalvo == NPCState.Morto)
                {
                    TransformToCorpse(true); 
                }
            }
        }
    }

    void Update()
    {
        if (currentState == NPCState.Morto) return;
        if (uiCanvasGroup == null) return;

        float targetAlpha = 0f; 
        bool canInteract = false;

        if (healthSystem != null && healthSystem.isFallen && playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= interactionRange)
            {
                targetAlpha = 1f; 
                canInteract = true;
            }
        }
        
        uiCanvasGroup.alpha = Mathf.MoveTowards(uiCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        
        if (uiCanvasGroup.alpha <= 0.01f && targetAlpha == 0f) 
        {
            if (decisionUIObject.activeSelf) decisionUIObject.SetActive(false);
        }
        else
        {
             if (!decisionUIObject.activeSelf) decisionUIObject.SetActive(true);
        }

        if (canInteract && input != null)
        {
            if (input.Player.Purify.WasPressedThisFrame()) SerPurificado();
            else if (input.Player.Kill.WasPressedThisFrame())
            {
                 if (healthSystem != null)
                 {
                    healthSystem.canBeKilledNormally = true; 
                    healthSystem.ApplyEffect(new Effect[] { new Effect { effectType = Effect.EffectType.physical, power = 9999 } });
                 }
            }
        }
    }

    public void EntrarEmNocaute()
    {
        if (currentState != NPCState.Corrompido) return;
        currentState = NPCState.Nocauteado;
    }

    public void SerPurificado()
    {
        currentState = NPCState.Purificado;
        HideUI(); 
        if (UIFeedbackManager.Instance != null) UIFeedbackManager.Instance.ShowNotification("Inimigo Purificado!", 2f);
        if (LevelingSystem.Instance != null) LevelingSystem.Instance.AddPurificationXP(xpReward);
        if (WorldStateManager.Instance != null) WorldStateManager.Instance.SetNPCState(npcID, NPCState.Purificado);
        if (rotaParaAbrir != null) rotaParaAbrir.SetActive(false);
        
        gameObject.SetActive(false);
    }

    public void SerMorto()
    {
        currentState = NPCState.Morto;
        HideUI();

        // --- ADIÇÃO CRÍTICA PARA QUESTS ---
        if (!string.IsNullOrEmpty(enemyTypeID))
        {
            // Avisa o sistema: "Um inimigo do tipo X morreu"
            GameEvents.TriggerEnemyDeath(enemyTypeID);
        }
        // ----------------------------------

        if (LevelingSystem.Instance != null) LevelingSystem.Instance.AddCombatXP(xpReward);
        if (WorldStateManager.Instance != null) WorldStateManager.Instance.SetNPCState(npcID, NPCState.Morto);
        
        TransformToCorpse(false); 
    }

    private void TransformToCorpse(bool instant)
    {
        if (myCollider) myCollider.enabled = false;
        if (agent) agent.enabled = false;
        var ai = GetComponent<EnemyAIController>();
        if (ai) ai.enabled = false;

        if (healthSystem && healthSystem.healthBarSlider) 
            healthSystem.healthBarSlider.gameObject.SetActive(false);

        if (animator)
        {
            if (instant) animator.Play("dead", 0, 1.0f); // Confira o nome no Animator!
        }
    }

    private void HideUI()
    {
        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 0f;
            decisionUIObject.SetActive(false);
        }
    }
    
    void OnValidate()
    {
        if (string.IsNullOrEmpty(npcID)) npcID = System.Guid.NewGuid().ToString();
    }
}