using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; 

public class MainMenu : MonoBehaviour
{
    // ... (todas as suas variáveis de Header/Referências permanecem as mesmas) ...
    [Header("Referências da UI")]
    public Button continueButton;
    public Button defaultSelectedButton; 
    [Header("Nomes das Cenas")]
    public string gameSceneName = "Vilarejo"; 

    private string saveFilePath;
    private GameObject lastSelectedGameObject;

    void Start()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (continueButton != null)
        {
            continueButton.interactable = File.Exists(saveFilePath);
        }

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToUIMap();
        }
        else
        {
             Debug.LogError("[MainMenu] InputManager.Instance não encontrado no Start!");
        }

        SelectDefaultButton();
    }

    void Update()
    {
        // ... (todo o seu código Update() e lógica de seleção permanece o mesmo) ...
        if (EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject == null)
        {
            bool isMouseMoving = Mouse.current != null && Mouse.current.delta.IsActuated(0.1f);
            if (!isMouseMoving) 
            {
                if (lastSelectedGameObject != null && lastSelectedGameObject.activeInHierarchy && lastSelectedGameObject.GetComponent<Selectable>()?.IsInteractable() == true)
                {
                    EventSystem.current.SetSelectedGameObject(lastSelectedGameObject);
                }
                else
                {
                    SelectDefaultButton();
                }
            }
        }
        else if (EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
        }
    }

    private void SelectDefaultButton()
    {
        if (EventSystem.current == null) return;

        if (defaultSelectedButton != null && defaultSelectedButton.interactable)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
            lastSelectedGameObject = defaultSelectedButton.gameObject;
        }
        else
        {
            Button firstInteractableButton = GetComponentInChildren<Button>(false);
            if (firstInteractableButton != null && firstInteractableButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(firstInteractableButton.gameObject);
                lastSelectedGameObject = firstInteractableButton.gameObject;
            }
             else 
             {
                 EventSystem.current.SetSelectedGameObject(null);
                 lastSelectedGameObject = null;
             }
        }
    }

    // --- MODIFICAÇÃO CHAVE ---
    public void StartNewGame()
    {
        // Limpa o estado e garante que a cena carregue com o Player no spawn inicial
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.ResetState();
        }
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetGameData();
        }
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToGameplayMap();
        }
        
        Debug.Log("INICIANDO NOVO JOGO...");

        // Usamos o TransitionManager para definir o spawn inicial
        if (TransitionManager.Instance != null)
        {
            // O ID "fase1_spawn" é o que você configurou para o seu SceneEntrance!
            TransitionManager.Instance.TransitionToScene(gameSceneName, "fase1_spawn");
        }
        else
        {
             // Fallback caso o TransitionManager não exista
            SceneManager.LoadScene(gameSceneName);
        }
    }
    // --- FIM DA MODIFICAÇÃO ---

    public void ContinueGame()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToGameplayMap();
        }
        Debug.Log("CONTINUANDO JOGO...");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }
        
        // Aqui, NÃO precisamos do TransitionManager, pois a cena já
        // é carregada pelo SaveManager, e a rotina de teleporte já funciona.
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