using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel; // O painel preto/fundo
    public TextMeshProUGUI dialogueText; // O texto da fala
    public TextMeshProUGUI nameText; // O nome de quem fala (opcional, pode ser "Eu" ou "Voz Misteriosa")
    public Image portraitImage; // Foto do personagem (opcional)

    [Header("Settings")]
    public float typingSpeed = 0.02f; // Velocidade do efeito de digitação

    private Queue<string> sentences;
    public bool IsDialogueActive { get; private set; }    private Coroutine typingCoroutine;
    
    // Cache do estado anterior do tempo
    private float previousTimeScale = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        sentences = new Queue<string>();
    }

    void Start()
    {
        if (dialoguePanel) dialoguePanel.SetActive(false);
    }

    // Chamado pelo Input (Mouse Click ou Botão de Interação)
    public void DisplayNextSentence()
    {
        if (!IsDialogueActive) return;

        // Se ainda está digitando, completa a frase imediatamente
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            // Mostra a frase inteira sem efeito
            if (dialogueText) dialogueText.text = currentSentence; 
            return;
        }

        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        string sentence = sentences.Dequeue();
        typingCoroutine = StartCoroutine(TypeSentence(sentence));
    }

    // --- FUNÇÃO PÚBLICA PARA INICIAR DIÁLOGO ---
    // Use esta função no UnityEvent do QuestEventTrigger!
    public void StartDialogue(string[] lines, string characterName = "")
    {
        if (IsDialogueActive) return; // Evita sobreposição

        IsDialogueActive = true;
        
        // Pausa o jogo (Opcional - remova se quiser diálogos em tempo real)
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Troca Input para UI
        if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
        
        // Destrava cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (nameText) nameText.text = string.IsNullOrEmpty(characterName) ? "Eu" : characterName;

        sentences.Clear();
        foreach (string line in lines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    // Sobrecarga simples para chamar com uma única string (útil para UnityEvents no Inspector)
    // O UnityEvent não suporta Array de strings direto, então usamos este helper.
    // Dica: Para conversas longas, crie um ScriptableObject de Diálogo no futuro.
    public void StartSingleLine(string line)
    {
        StartDialogue(new string[] { line });
    }

    private string currentSentence;
    
    IEnumerator TypeSentence(string sentence)
    {
        currentSentence = sentence;
        dialogueText.text = "";
        
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            // Usa WaitForSecondsRealtime porque o TimeScale pode estar 0
            yield return new WaitForSecondsRealtime(typingSpeed); 
        }
        
        typingCoroutine = null;
    }

    void EndDialogue()
    {
        IsDialogueActive = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);

        // Restaura o tempo
        Time.timeScale = previousTimeScale;

        // Restaura Input
        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
        
        // Restaura Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}