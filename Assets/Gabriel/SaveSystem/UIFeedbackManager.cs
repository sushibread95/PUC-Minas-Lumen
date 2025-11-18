// Nome do arquivo: UIFeedbackManager.cs
// CÓDIGO COMPLETO E CORRIGIDO (COM LÓGICA DE REGISTRO)

using UnityEngine;
using TMPro; 
using System.Collections;
using UnityEngine.SceneManagement; // --- ADICIONADO ---

public class UIFeedbackManager : MonoBehaviour
{
    public static UIFeedbackManager Instance { get; private set; }

    // --- MODIFICAÇÃO ---
    // Agora é privado. Será preenchido pelo script 'FeedbackUIRegistrar'
    private TextMeshProUGUI notificationText;
    // --- FIM DA MODIFICAÇÃO ---
    
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
        // --- ADIÇÃO ---
        // Se inscreve no evento de 'cena carregada'
        SceneManager.sceneLoaded += OnSceneLoaded;
        // --- FIM DA ADIÇÃO ---
    }

    void OnDisable()
    {
        SaveManager.OnGameSaved -= ShowSaveNotification;
        // --- ADIÇÃO ---
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // --- FIM DA ADIÇÃO ---
    }

    // --- ADIÇÃO ---
    // Chamado toda vez que uma nova cena é carregada
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Limpa a referência antiga. O novo texto da UI na nova cena
        // será forçado a se registrar novamente.
        notificationText = null;
    }
    // --- FIM DA ADIÇÃO ---

    // --- ADIÇÃO ---
    // O novo script (FeedbackUIRegistrar) vai chamar esta função
    public void RegisterFeedbackText(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            notificationText = textComponent;
            notificationText.gameObject.SetActive(false); // Garante que comece desligado
            Debug.Log("UIFeedbackManager: Texto de notificação registrado com sucesso.");
        }
    }
    // --- FIM DA ADIÇÃO ---

    void ShowSaveNotification()
    {
        ShowNotification("Salvando...", defaultDuration);
    }

    public void ShowNotification(string message, float duration)
    {
        // --- MODIFICAÇÃO ---
        // Checa se o 'notificationText' foi registrado
        if (notificationText != null)
        {
            if (currentFeedbackRoutine != null)
            {
                StopCoroutine(currentFeedbackRoutine);
            }
            currentFeedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message, duration));
        }
        else
        {
            Debug.LogWarning($"UIFeedbackManager: Quis mostrar '{message}', mas nenhum 'notificationText' foi registrado nesta cena.");
        }
        // --- FIM DA MODIFICAÇÃO ---
    }

    private IEnumerator ShowFeedbackRoutine(string message, float duration)
    {
        notificationText.gameObject.SetActive(true);
        notificationText.text = message;

        yield return new WaitForSecondsRealtime(duration); 

        notificationText.text = "";
        notificationText.gameObject.SetActive(false);
        currentFeedbackRoutine = null;
    }
}