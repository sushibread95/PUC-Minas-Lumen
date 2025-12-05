using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI; 

public class CorruptedNPC : MonoBehaviour
{
    [Header("Identificação")]
    public string npcID;

    [Header("Recompensas")]
    public float xpReward = 50f;

    [Header("UI de Decisão")]
    [Tooltip("Arraste o GameObject 'DecisionCanvas' aqui. O script cuida do resto.")]
    public GameObject decisionUIObject; // --- VOLTAMOS PARA GAMEOBJECT (MAIS FÁCIL DE ARRASTAR) ---
    public float interactionRange = 3.0f;
    public float fadeSpeed = 5.0f;

    [Header("Estado")]
    public NPCState currentState = NPCState.Corrompido;
    public GameObject rotaParaAbrir;

    // Variáveis Privadas
    private CanvasGroup uiCanvasGroup; // --- USAMOS ESTE PARA A LÓGICA INTERNA ---
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
        // 1. Configuração Automática da UI
        if (decisionUIObject != null)
        {
            // Tenta pegar o CanvasGroup
            uiCanvasGroup = decisionUIObject.GetComponent<CanvasGroup>();
            
            // Se não tiver, adiciona automaticamente para evitar erros
            if (uiCanvasGroup == null)
            {
                uiCanvasGroup = decisionUIObject.AddComponent<CanvasGroup>();
            }

            // Inicializa invisível
            uiCanvasGroup.alpha = 0f;
            decisionUIObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"CorruptedNPC ({name}): O campo 'Decision UI Object' está vazio no Inspector!");
        }

        // 2. Cache do Player e Input
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerTransform = playerObj.transform;
        if (InputManager.Instance != null) input = InputManager.Instance.InputActions;

        // 3. Carregar Save
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
        
        // Se não configurou a UI, aborta para não dar erro
        if (uiCanvasGroup == null) return;

        float targetAlpha = 0f; 
        bool canInteract = false;

        // Lógica de aparecer a UI
        if (healthSystem != null && healthSystem.isFallen && playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= interactionRange)
            {
                targetAlpha = 1f; 
                canInteract = true;
            }
        }
        
        // Aplica Fade usando a variável privada uiCanvasGroup
        uiCanvasGroup.alpha = Mathf.MoveTowards(uiCanvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        
        // Otimização
        if (uiCanvasGroup.alpha <= 0.01f && targetAlpha == 0f) 
        {
            if (decisionUIObject.activeSelf) decisionUIObject.SetActive(false);
        }
        else
        {
             if (!decisionUIObject.activeSelf) decisionUIObject.SetActive(true);
        }

        // Inputs
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

    // --- FUNÇÕES DE ESTADO ---

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
            if (instant)
            {
                // Mude "dead" para o nome exato do estado no seu Animator se for diferente
                animator.Play("dead", 0, 1.0f); 
            }
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