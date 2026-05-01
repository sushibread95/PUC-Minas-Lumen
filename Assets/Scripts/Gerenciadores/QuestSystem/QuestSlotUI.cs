using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    [Header("Componentes Visuais")]
    public TextMeshProUGUI titleText;       // Título da Quest
    public TextMeshProUGUI descriptionText; // Descrição do passo atual
    public TextMeshProUGUI progressText;    // "2/5"
    public Image statusIcon;                // Ícone (opcional)

    public void Setup(QuestSaveData data, QuestDefinition def)
    {
        if (def == null) return;

        // 1. Título
        titleText.text = def.title;

        // 2. Cores baseadas no estado
        if (data.isCompleted)
        {
            titleText.color = Color.green; // Ou cinza escuro
            if (descriptionText) descriptionText.text = "Missão Concluída.";
            if (progressText) progressText.text = "V";
        }
        else
        {
            titleText.color = Color.white; // Ou dourado
            
            // 3. Descrição do Passo Atual
            if (data.currentStepIndex < def.steps.Count)
            {
                var step = def.steps[data.currentStepIndex];
                if (descriptionText) descriptionText.text = step.description;

                // 4. Progresso Numérico (Só mostra se for > 1, ex: Matar 5)
                if (step.amountRequired > 1 && progressText)
                {
                    progressText.text = $"{data.currentAmount}/{step.amountRequired}";
                }
                else if (progressText)
                {
                    progressText.text = ""; 
                }
            }
        }
    }
}