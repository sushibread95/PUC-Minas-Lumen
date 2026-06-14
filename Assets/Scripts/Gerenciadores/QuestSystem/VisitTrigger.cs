using UnityEngine;

// Zona de "chegada a um local" para objetivos de quest do tipo Visit.
// Coloque este componente em um GameObject vazio com um BoxCollider (Is Trigger = true)
// posicionado na entrada do local (mercado, igreja, saída para o castelo, etc.).
// Quando o Player entra na zona, dispara GameEvents.OnLocationVisited(locationID),
// que o QuestManager usa para avançar o passo Visit correspondente.
[RequireComponent(typeof(BoxCollider))]
public class VisitTrigger : MonoBehaviour
{
    [Header("--- IDENTIDADE DO LOCAL ---")]
    [Tooltip("Deve ser IGUAL ao 'targetID' do passo Visit na QuestDefinition. Ex: 'mercado', 'igreja', 'saida_castelo'.")]
    public string locationID = "local_id";

    [Header("--- DICA AO CHEGAR CEDO ---")]
    [Tooltip("Mensagem mostrada se o player chega aqui ANTES de cumprir o passo anterior da quest. " +
             "Deixe VAZIO para usar a dica automática (a descrição do passo atual da quest).")]
    [TextArea] public string lockedHint = "";

    [Tooltip("Quanto tempo a dica fica na tela (segundos).")]
    public float hintDuration = 3f;

    [Tooltip("Intervalo mínimo entre dicas, para não repetir a cada passo dentro da zona (segundos).")]
    public float hintCooldown = 4f;

    [Header("--- SAVE / ÚNICA VEZ ---")]
    [Tooltip("Se preenchido, o WorldStateManager registra a visita e a zona não dispara de novo após recarregar a cena. Se vazio, dispara sempre que o Player entrar.")]
    public string uniqueVisitID = "";

    private bool hasTriggered = false;
    private float lastHintTime = -999f;

    private void Reset()
    {
        // Garante que o collider já venha como trigger ao adicionar o componente.
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
    }

    private void Start()
    {
        // Se esta visita já aconteceu num save anterior, não dispara de novo.
        if (!string.IsNullOrEmpty(uniqueVisitID) && WorldStateManager.Instance != null)
        {
            if (WorldStateManager.Instance.HasEventHappened(uniqueVisitID))
                hasTriggered = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Mesmo padrão de checagem de Player usado no QuestEventTrigger.
        if (!(other.CompareTag("Player") || other.GetComponent<PlayerPersistent>()))
            return;

        QuestManager qm = QuestManager.Instance;
        QuestManager.ObjectiveProgress status = qm != null
            ? qm.GetObjectiveProgress(ObjectiveType.Visit, locationID)
            : QuestManager.ObjectiveProgress.None;

        switch (status)
        {
            // É exatamente o passo que falta: conta a visita (uma única vez).
            case QuestManager.ObjectiveProgress.Current:
                if (hasTriggered) return;
                hasTriggered = true;

                if (!string.IsNullOrEmpty(uniqueVisitID) && WorldStateManager.Instance != null)
                    WorldStateManager.Instance.RegisterEventTriggered(uniqueVisitID);

                GameEvents.TriggerLocationVisited(locationID);
                break;

            // Chegou cedo: mostra a dica, mas NÃO trava — conta quando voltar na hora certa.
            case QuestManager.ObjectiveProgress.Future:
                ShowEarlyHint(qm);
                break;

            // Já feito, ou nenhuma quest ativa precisa deste local agora: ignora
            // sem travar, para não atrapalhar uma ativação futura do passo.
            case QuestManager.ObjectiveProgress.Done:
            case QuestManager.ObjectiveProgress.None:
            default:
                break;
        }
    }

    // Mostra a dica de "ainda não posso seguir", com cooldown para não repetir.
    private void ShowEarlyHint(QuestManager qm)
    {
        if (Time.time - lastHintTime < hintCooldown) return;
        lastHintTime = Time.time;

        string msg = lockedHint;
        if (string.IsNullOrEmpty(msg))
        {
            // Dica automática: usa a descrição do passo que falta cumprir.
            string blocking = qm.GetBlockingStepDescription(ObjectiveType.Visit, locationID);
            msg = string.IsNullOrEmpty(blocking)
                ? "Ainda não posso seguir por aqui."
                : $"Antes disso preciso: {blocking}";
        }

        if (UIFeedbackManager.Instance != null)
            UIFeedbackManager.Instance.ShowNotification(msg, hintDuration);
    }
}
