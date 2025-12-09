using UnityEngine;
using UnityEngine.InputSystem;

public class QuestDebugTrigger : MonoBehaviour
{
    [Header("Teste de Quest")]
    [Tooltip("ID da Quest que criamos no ScriptableObject (Ex: quest_inicio)")]
    public string questIDToStart = "quest_inicio"; 

    void Update()
    {
        // Pressione K para simular aceitar a missão
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (QuestManager.Instance != null)
            {
                Debug.Log($"[DEBUG] Solicitando início da quest: {questIDToStart}");
                QuestManager.Instance.AcceptQuest(questIDToStart);
            }
            else
            {
                Debug.LogError("[DEBUG] QuestManager não encontrado! Verifique a cena Boot.");
            }
        }

        // Pressione L para imprimir o status atual das quests no console
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            if (QuestManager.Instance != null)
            {
                var active = QuestManager.Instance.GetActiveQuestsSaveData();
                Debug.Log($"--- QUESTS ATIVAS: {active.Count} ---");
                foreach(var q in active)
                {
                    Debug.Log($"Quest: {q.questID} | Passo: {q.currentStepIndex} | Progresso: {q.currentAmount}");
                }
            }
        }
    }
}