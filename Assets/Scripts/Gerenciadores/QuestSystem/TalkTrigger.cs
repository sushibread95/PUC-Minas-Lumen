using UnityEngine;
using UnityEngine.Events;

// Ponto de "conversar / examinar" para objetivos de quest do tipo Talk.
// Implementa IInteractable, então funciona com o InteractionManager existente:
// o player olha para o objeto, vê o prompt "[E] ..." e aperta E.
// Coloque em um NPC, num aldeão caído, num pergaminho ou em qualquer objeto examinável.
// Ao interagir, dispara GameEvents.OnNPCTalked(talkID), avançando o passo Talk da quest.
public class TalkTrigger : MonoBehaviour, IInteractable
{
    [Header("--- IDENTIDADE ---")]
    [Tooltip("Deve ser IGUAL ao 'targetID' do passo Talk na QuestDefinition. Ex: 'aldeao_entrada'.")]
    public string talkID = "npc_id";

    [Tooltip("Texto mostrado no prompt de interação. Ex: 'Examinar', 'Conversar'.")]
    public string interactText = "Examinar";

    [Header("--- FEEDBACK / NARRATIVA ---")]
    [Tooltip("Mensagem opcional mostrada na tela ao interagir. Deixe vazio para não mostrar nada.")]
    [TextArea] public string messageOnTalk = "";

    [Tooltip("Quanto tempo a mensagem fica na tela (segundos).")]
    public float messageDuration = 3f;

    [Tooltip("Eventos extras ao interagir (ex: tocar um diálogo pelo DialogueManager, ativar uma cinemática).")]
    public UnityEvent onTalk;

    [Header("--- SAVE / ÚNICA VEZ ---")]
    [Tooltip("Se preenchido, registra no WorldStateManager e o objeto não responde de novo após recarregar a cena. Se vazio, pode interagir sempre.")]
    public string uniqueTalkID = "";

    private bool hasTalked = false;

    private void Start()
    {
        if (!string.IsNullOrEmpty(uniqueTalkID) && WorldStateManager.Instance != null)
        {
            if (WorldStateManager.Instance.HasEventHappened(uniqueTalkID))
                hasTalked = true;
        }
    }

    // IInteractable: texto do prompt.
    public string GetInteractText()
    {
        return interactText;
    }

    // IInteractable: ação ao apertar E.
    public void Interact()
    {
        // Avança a quest (mesmo que já tenha falado antes, o QuestManager ignora
        // se não for o passo atual — então é seguro disparar).
        GameEvents.TriggerNPCTalked(talkID);

        if (!string.IsNullOrEmpty(messageOnTalk) && UIFeedbackManager.Instance != null)
            UIFeedbackManager.Instance.ShowNotification(messageOnTalk, messageDuration);

        onTalk?.Invoke();

        if (!string.IsNullOrEmpty(uniqueTalkID) && WorldStateManager.Instance != null && !hasTalked)
        {
            WorldStateManager.Instance.RegisterEventTriggered(uniqueTalkID);
            hasTalked = true;
        }
    }
}
