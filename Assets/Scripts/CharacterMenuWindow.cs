using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // Necessário para seleção
using System.Collections;

public class CharacterMenuWindow : MonoBehaviour
{
    public static CharacterMenuWindow Instance;

    [Header("UI References")]
    public GameObject menuPanelObject; // O GameObject pai (para ligar/desligar)
    public CanvasGroup menuCanvasGroup; // --- ADIÇÃO: Para controle de interação ---

    [Header("Abas de Conteúdo")]
    // 0: Inventário, 1: Equipamentos, etc.
    public GameObject[] pages;

    [Header("Popups")]
    public GameObject actionPanel; 

    [Header("Navegação Visual")]
    public Image[] tabBackgrounds;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;

    public bool IsMenuOpen => menuPanelObject != null && menuPanelObject.activeSelf;

    private int currentPageIndex = 0;
    private PlayerInputActions input;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // Configuração inicial segura
        if (menuPanelObject) menuPanelObject.SetActive(false);
        if (actionPanel) actionPanel.SetActive(false); 
        
        // Garante referência do CanvasGroup
        if (menuCanvasGroup == null && menuPanelObject != null)
            menuCanvasGroup = menuPanelObject.GetComponent<CanvasGroup>();
    }

    void Start()
    {
        if (InputManager.Instance != null)
        {
            input = InputManager.Instance.InputActions;
            // Configura Q e E para trocar abas apenas se o menu estiver aberto
            var uiMap = input.UI; // Usando referência direta ao mapa
            uiMap.NextTab.performed += ctx => ChangeTab(1);
            uiMap.PrevTab.performed += ctx => ChangeTab(-1);
        }
    }

    // Input de UI geralmente funciona mesmo pausado, então Update é seguro aqui
    void Update()
    {
        if (menuPanelObject == null || !menuPanelObject.activeSelf) return;
        
        // Fallback de teclado para abas
        if (Keyboard.current.eKey.wasPressedThisFrame) ChangeTab(1);
        if (Keyboard.current.qKey.wasPressedThisFrame) ChangeTab(-1);
    }

    public void ToggleMenu()
    {
        bool isOpening = !menuPanelObject.activeSelf;

        if (isOpening)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu();
        }
    }

    private void OpenMenu()
    {
        menuPanelObject.SetActive(true);
        if (menuCanvasGroup)
        {
            menuCanvasGroup.alpha = 1f;
            menuCanvasGroup.interactable = true;
            menuCanvasGroup.blocksRaycasts = true;
        }

        Time.timeScale = 0f; // Pausa o jogo
        
        // Configura Inputs
        if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
        
        // Destrava Cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (actionPanel) actionPanel.SetActive(false);

        // Atualiza a aba atual (Isso vai chamar a lógica do Inventário se for a aba 0)
        UpdateUI();
    }

    public void CloseMenu()
    {
        if (menuCanvasGroup)
        {
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.interactable = false;
            menuCanvasGroup.blocksRaycasts = false;
        }
        menuPanelObject.SetActive(false);

        Time.timeScale = 1f; // Despausa

        if (actionPanel) actionPanel.SetActive(false);

        // Desliga todas as páginas visualmente
        foreach (var page in pages)
        {
            if (page != null) page.SetActive(false);
        }

        // Restaura Gameplay
        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
        
        // Trava Cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Limpa seleção do EventSystem para não ficar "fantasma"
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void ChangeTab(int direction)
    {
        if (!menuPanelObject.activeSelf) return;
        currentPageIndex += direction;
        if (currentPageIndex >= pages.Length) currentPageIndex = 0;
        else if (currentPageIndex < 0) currentPageIndex = pages.Length - 1;
        UpdateUI();
    }

    public void OpenSpecificTab(int index)
    {
        if (index < 0 || index >= pages.Length) return;
        currentPageIndex = index;
        // Se o menu já estiver aberto, só atualiza. Se não, o ToggleMenu cuida disso.
        if (menuPanelObject.activeSelf) UpdateUI();
    }

    private void UpdateUI()
    {
        if (actionPanel != null) actionPanel.SetActive(false);

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null) continue;
            
            bool isActive = (i == currentPageIndex);
            pages[i].SetActive(isActive);

            // --- INTEGRAÇÃO COM CONTROLLERS ---
            // Se ativamos a aba de Inventário (assumindo index 0), avisamos o controller
            if (isActive && i == 0 && InventoryController.Instance != null)
            {
                InventoryController.Instance.OnInventoryTabOpened();
            }

            if (isActive && QuestUIController.Instance != null && i == 1) // Troque 1 pelo índice correto da sua aba
            {
                QuestUIController.Instance.OnQuestTabOpened();
            }

        }

        // Cores dos botões de aba
        if (tabBackgrounds != null)
        {
            for (int i = 0; i < tabBackgrounds.Length; i++)
            {
                if (tabBackgrounds[i] != null)
                    tabBackgrounds[i].color = (i == currentPageIndex) ? activeTabColor : inactiveTabColor;
            }
        }
    }
}