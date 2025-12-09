using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

[RequireComponent(typeof(BoxCollider))]
public class QuestEventTrigger : MonoBehaviour
{
    [Header("Identidade (Save System)")]
    [Tooltip("Dê um nome ÚNICO se quiser que este evento toque apenas UMA vez na vida (ex: 'dialogo_entrada_caverna'). Se deixar vazio, ele repete sempre que carregar a cena.")]
    public string uniqueTriggerID; // --- NOVO CAMPO ---

    [Header("1. Configuração da Quest")]
    public string questID;
    public bool onlyIfQuestNotStarted = true;

    [Header("2. Opções de Evento")]
    public PlayableDirector cinematicDirector;
    public GameObject cameraToActivate;
    public float cameraDuration = 3f; 
    public UnityEvent onTriggerEnterEvent;

    private bool hasTriggered = false;

    // --- NOVO: CHECAGEM NO START ---
    void Start()
    {
        // Se este gatilho tem um ID e o jogo diz que já aconteceu...
        if (!string.IsNullOrEmpty(uniqueTriggerID) && WorldStateManager.Instance != null)
        {
            if (WorldStateManager.Instance.HasEventHappened(uniqueTriggerID))
            {
                // ...destrói o gatilho imediatamente.
                gameObject.SetActive(false); 
            }
        }
    }
    // -------------------------------

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        
        if (other.CompareTag("Player") || other.GetComponent<PlayerPersistent>())
        {
            ValidateAndTrigger();
        }
    }

    private void ValidateAndTrigger()
    {
        // Validação de Quest (Mantida)
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(questID))
        {
            bool isStarted = false;
            var activeQuests = QuestManager.Instance.GetActiveQuestsSaveData();
            foreach (var q in activeQuests) if (q.questID == questID) isStarted = true;
            
            var completedQuests = QuestManager.Instance.GetCompletedQuestsSaveData();
            bool isCompleted = completedQuests.Contains(questID);

            if (onlyIfQuestNotStarted && (isStarted || isCompleted)) return;
        }

        // --- REGISTRO NO SAVE SYSTEM ---
        if (!string.IsNullOrEmpty(uniqueTriggerID) && WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.RegisterEventTriggered(uniqueTriggerID);
        }
        // -------------------------------

        hasTriggered = true;
        
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(questID)) 
            QuestManager.Instance.AcceptQuest(questID);

        if (cinematicDirector != null) cinematicDirector.Play();

        if (cameraToActivate != null) StartCoroutine(CameraSwitchRoutine());

        onTriggerEnterEvent?.Invoke();
        
        GetComponent<Collider>().enabled = false;
    }

    private System.Collections.IEnumerator CameraSwitchRoutine()
    {
        cameraToActivate.SetActive(true);
        yield return new WaitForSeconds(cameraDuration);
        cameraToActivate.SetActive(false);
    }
}