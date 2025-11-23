using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class CharacterMenuWindow : MonoBehaviour
{
    public static CharacterMenuWindow Instance;

    [Header("Painel Principal")]
    public GameObject menuPanel; // O objeto "MenuUI"

    [Header("Abas de Conteúdo")]
    // Element 0: IventoryCanva
    // Element 1: Equipamentos
    public GameObject[] pages;

    [Header("Popups (Para fechar ao trocar de aba)")]
    public GameObject actionPanel; // O objeto "ActionPanel"

    [Header("Visual dos Botões (Opcional)")]
    public Image[] tabBackgrounds;
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;

    private int currentPageIndex = 0;
    private PlayerInputActions input;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (menuPanel) menuPanel.SetActive(false);
        if (actionPanel) actionPanel.SetActive(false); // Garante que começa fechado
    }

    void Start()
    {
        if (InputManager.Instance != null)
        {
            input = InputManager.Instance.InputActions;
            // Configura Q e E para trocar abas
            var uiMap = input.UI.Get();
            uiMap.FindAction("NextTab").performed += ctx => ChangeTab(1);
            uiMap.FindAction("PrevTab").performed += ctx => ChangeTab(-1);
        }
    }

    void Update()
    {
        if (!menuPanel.activeSelf) return;
        // Fallback teclado
        if (Keyboard.current.eKey.wasPressedThisFrame) ChangeTab(1);
        if (Keyboard.current.qKey.wasPressedThisFrame) ChangeTab(-1);
    }

    public void ToggleMenu()
    {
        bool isActive = !menuPanel.activeSelf;
        menuPanel.SetActive(isActive);

        if (isActive) // --- ABRINDO O MENU ---
        {
            Time.timeScale = 0f;

            // Se for a primeira vez ou quiser resetar, abre no inventário
            // if (currentPageIndex == 0) OpenSpecificTab(0); 

            // Atualiza para mostrar a aba correta
            UpdateUI();

            // Garante que o ActionPanel comece fechado
            if (actionPanel) actionPanel.SetActive(false);

            if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else // --- FECHANDO O MENU ---
        {
            Time.timeScale = 1f;

            // 1. Fecha o ActionPanel
            if (actionPanel) actionPanel.SetActive(false);

            // 2. CORREÇÃO: Força todas as páginas (Inventário, Equip, etc) a sumirem
            // Isso resolve o problema se elas não forem filhas do menuPanel
            foreach (var page in pages)
            {
                if (page != null) page.SetActive(false);
            }

            if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void ChangeTab(int direction)
    {
        if (!menuPanel.activeSelf) return;
        currentPageIndex += direction;
        if (currentPageIndex >= pages.Length) currentPageIndex = 0;
        else if (currentPageIndex < 0) currentPageIndex = pages.Length - 1;
        UpdateUI();
    }

    public void OpenSpecificTab(int index)
    {
        if (index < 0 || index >= pages.Length) return;
        currentPageIndex = index;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 1. Sempre fecha o ActionPanel ao mudar de aba
        if (actionPanel != null) actionPanel.SetActive(false);

        // 2. Liga a página certa
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == currentPageIndex);
        }

        // 3. Atualiza cores dos botões (se tiver)
        if (tabBackgrounds != null && tabBackgrounds.Length > 0)
        {
            for (int i = 0; i < tabBackgrounds.Length; i++)
            {
                if (tabBackgrounds[i] != null)
                    tabBackgrounds[i].color = (i == currentPageIndex) ? activeTabColor : inactiveTabColor;
            }
        }
    }
}