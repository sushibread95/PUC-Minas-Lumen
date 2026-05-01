using UnityEngine;
using TMPro;

public class FeedbackUIRegistrar : MonoBehaviour
{
    [Tooltip("Arraste o componente de Texto do seu HUD aqui")]
    public TextMeshProUGUI notificationText;

    void Start()
    {
        // Avisa o Manager global que este é o texto da vez
        if (UIFeedbackManager.Instance != null)
        {
            UIFeedbackManager.Instance.RegisterFeedbackText(notificationText);
        }
    }
}