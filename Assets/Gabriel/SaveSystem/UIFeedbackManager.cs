using UnityEngine;
using TMPro; 
using System.Collections;
using UnityEngine.SceneManagement;

public class UIFeedbackManager : MonoBehaviour
{
    public static UIFeedbackManager Instance { get; private set; }

    // --- MANTIDO COMO PRIVADO, MAS PREENCHIDO PELO REGISTRAR ---
    private TextMeshProUGUI notificationText;
    
    public float defaultDuration = 2f;
    private Coroutine currentFeedbackRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SaveManager.OnGameSaved += ShowSaveNotification;
    }

    void OnDisable()
    {
        SaveManager.OnGameSaved -= ShowSaveNotification;
    }

    // --- A ÚNICA ALTERAÇÃO NECESSÁRIA ---
    // Removemos o método "OnSceneLoaded" que limpava a referência.
    // Agora, uma vez registrado na cena Boot, ele lembra para sempre.
    // -------------------------------------

    public void RegisterFeedbackText(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            notificationText = textComponent;
            notificationText.gameObject.SetActive(false); 
        }
    }

    void ShowSaveNotification()
    {
        ShowNotification("Salvando...", defaultDuration);
    }

    public void ShowNotification(string message, float duration)
    {
        if (notificationText != null)
        {
            if (currentFeedbackRoutine != null)
            {
                StopCoroutine(currentFeedbackRoutine);
            }
            
            // Garante que o texto seja ativado antes de mudar o conteúdo
            notificationText.gameObject.SetActive(true);
            
            currentFeedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message, duration));
        }
        else
        {
            // Log de aviso apenas para debug, sem quebrar nada
            Debug.LogWarning($"UIFeedbackManager: '{message}' não exibida (notificationText nulo).");
        }
    }

    private IEnumerator ShowFeedbackRoutine(string message, float duration)
    {
        notificationText.text = message;
        
        // Garante a visibilidade novamente por segurança
        if (!notificationText.gameObject.activeSelf) 
            notificationText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(duration); 

        notificationText.text = "";
        notificationText.gameObject.SetActive(false);
        currentFeedbackRoutine = null;
    }
}