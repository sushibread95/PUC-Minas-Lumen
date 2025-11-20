using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; 

public class MainMenu : MonoBehaviour
{
    [Header("Referências da UI")]
    public Button continueButton;
    public Button defaultSelectedButton; 
    
    [Header("Nomes das Cenas")]
    public string gameSceneName = "Vilarejo"; 

    private string saveFilePath;
    private GameObject lastSelectedGameObject;

    void Start()
    {
        // --- CORREÇÃO 1: Garante cursor livre e visível ao abrir o menu ---
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // --- CORREÇÃO 2: Força o Input System a usar o mapa de UI ---
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToUIMap();
        }
        else
        {
             // Isso pode acontecer se você abrir a cena do Menu direto sem passar pelo Boot.
             // Não é crítico para testes, mas idealmente inicie pelo Boot.
             Debug.LogWarning("[MainMenu] InputManager.Instance não encontrado. Certifique-se de iniciar pela cena de Boot.");
        }

        // Lógica do Save
        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (continueButton != null)
        {
            continueButton.interactable = File.Exists(saveFilePath);
        }

        SelectDefaultButton();
    }

    void Update()
    {
        if (EventSystem.current == null) return;

        // Lógica para garantir que sempre tenha um botão selecionado (navegação por controle/teclado)
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
            // Tenta achar o primeiro botão disponível
            Button firstInteractableButton = GetComponentInChildren<Button>(false);
            if (firstInteractableButton != null && firstInteractableButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(firstInteractableButton.gameObject);
                lastSelectedGameObject = firstInteractableButton.gameObject;
            }
        }
    }

    public void StartNewGame()
    {
        // Limpa o estado para garantir um jogo novo limpo
        if (WorldStateManager.Instance != null)
        {
            WorldStateManager.Instance.ResetState();
        }
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetGameData();
        }
        
        // Muda o input para Gameplay, pois a próxima cena é o jogo
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToGameplayMap();
        }
        
        Debug.Log("INICIANDO NOVO JOGO...");

        if (TransitionManager.Instance != null)
        {
            // Define o spawn inicial
            TransitionManager.Instance.TransitionToScene(gameSceneName, "fase1_spawn");
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void ContinueGame()
    {
        // Muda o input para Gameplay
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SwitchToGameplayMap();
        }
        
        Debug.Log("CONTINUANDO JOGO...");

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