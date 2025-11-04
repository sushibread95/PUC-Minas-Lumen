using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems; // Necessário para EventSystem
using UnityEngine.InputSystem; // <<< NECESSÁRIO para Mouse.current

public class MainMenu : MonoBehaviour
{
    [Header("Referências da UI")]
    public Button continueButton;
    public Button defaultSelectedButton; // Arraste o BtnStart aqui

    [Header("Nomes das Cenas")]
    public string gameSceneName = "Vilarejo"; // Verifique se este é o nome correto

    private string saveFilePath;
    private GameObject lastSelectedGameObject;

    void Start()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");

        // Habilita/Desabilita botão Continuar
        if (continueButton != null)
        {
            continueButton.interactable = File.Exists(saveFilePath);
        }

        // Garante que o InputManager está no modo UI
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToUIMap();
        }
        else
        {
             Debug.LogError("[MainMenu] InputManager.Instance não encontrado no Start!");
        }

        // Garante a seleção inicial para o controle/teclado
        SelectDefaultButton();
    }

    void Update()
    {
        // Só executa se o EventSystem existir
        if (EventSystem.current == null) return;

        // Lógica para restaurar seleção do controle após interação do mouse
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            // Verifica se o mouse está parado usando o NOVO Input System
            bool isMouseMoving = Mouse.current != null && Mouse.current.delta.IsActuated(0.1f); // 0.1f = pequena tolerância

            if (!isMouseMoving) // Se o mouse está parado
            {
                // Re-seleciona o último botão que estava selecionado antes do mouse interferir,
                // ou o botão padrão se for a primeira vez.
                if (lastSelectedGameObject != null && lastSelectedGameObject.activeInHierarchy && lastSelectedGameObject.GetComponent<Selectable>()?.IsInteractable() == true)
                {
                    EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                }
                else
                {
                    SelectDefaultButton(); // Tenta selecionar o botão padrão novamente
                }
            }
        }
        else if (EventSystem.current.currentSelectedGameObject != null)
        {
            // Atualiza o último objeto selecionado (quando usando controle/teclado)
            lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
        }
    }

    // Função helper para selecionar o botão padrão
    private void SelectDefaultButton()
    {
        // Só executa se o EventSystem existir
        if (EventSystem.current == null) return;

        if (defaultSelectedButton != null && defaultSelectedButton.interactable) // Verifica se é interativo
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
            lastSelectedGameObject = defaultSelectedButton.gameObject;
        }
        else // Fallback se não configurado ou não interativo (ex: Continue desabilitado)
        {
             // Tenta selecionar o primeiro botão interativo filho do Canvas
            Button firstInteractableButton = GetComponentInChildren<Button>(false); // false = não incluir inativos
            if (firstInteractableButton != null && firstInteractableButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(firstInteractableButton.gameObject);
                lastSelectedGameObject = firstInteractableButton.gameObject;
            }
             else // Se nenhum botão for encontrado/interativo
             {
                 EventSystem.current.SetSelectedGameObject(null); // Garante que nada esteja selecionado
                 lastSelectedGameObject = null;
             }
        }
    }

    public void StartNewGame()
    {
        // --- LIMPA O ESTADO DOS MANAGERS ---
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.ResetState();
        }
        // (Adicionar chamada para InventoryManager.Instance.ResetState(); aqui no futuro)
        // -----------------------------------

        // Troca para o mapa Gameplay ANTES de carregar a cena
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToGameplayMap();
        }
        Debug.Log("INICIANDO NOVO JOGO...");
        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinueGame()
    {
        // Troca para o mapa Gameplay ANTES de carregar a cena
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToGameplayMap();
        }
        Debug.Log("CONTINUANDO JOGO...");

        // Carrega os dados ANTES de carregar a cena
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Fechando o jogo...");
        Application.Quit();
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }
}