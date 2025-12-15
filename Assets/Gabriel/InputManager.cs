using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // A "Fonte da Verdade". A ÚNICA instância de input no jogo.
    public PlayerInputActions InputActions { get; private set; }

    void Awake()
    {
        // 1. Singleton + DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 2. Cria a ÚNICA instância
        InputActions = new PlayerInputActions();
        
        // 3. O jogo sempre começa no modo "UI" (para o MainMenu)
        SwitchToUIMap();
    }

    public void SwitchToGameplayMap()
    {
        Debug.Log("🎮 INPUT MANAGER: Trocando para o Mapa 'Player'");
        InputActions.Player.Enable();
        InputActions.UI.Disable();
    }

    public void SwitchToUIMap()
    {
        Debug.Log("📋 INPUT MANAGER: Trocando para o Mapa 'UI'");
        InputActions.UI.Enable();
        InputActions.Player.Disable();
    }

    // ✅ CORREÇÃO CRÍTICA: Destruição Apropriada
    private void OnDestroy()
    {
        Debug.Log("⚠️ InputManager sendo destruído. Limpando InputActions...");
        
        if (InputActions != null)
        {
            // Desabilita os mapas primeiro
            InputActions.Player.Disable();
            InputActions.UI.Disable();
            
            // Destroi o asset (isso libera os recursos)
            InputActions.Dispose();
            InputActions = null;
        }
        
        // Limpa a referência do Singleton
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ✅ ADICIONAL: Limpeza ao desabilitar (segurança extra)
    private void OnDisable()
    {
        if (InputActions != null)
        {
            InputActions.Player.Disable();
            InputActions.UI.Disable();
        }
    }
}