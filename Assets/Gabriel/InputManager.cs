using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // A "Fonte da Verdade". A ÚNICA instância de input no jogo.
    public PlayerInputActions InputActions { get; private set; }

    // Nomes dos mapas (para evitar erros de digitação)
    //private string MAP_PLAYER = "Player";
    //private string MAP_UI = "UI";

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
        Debug.LogWarning("INPUT MANAGER: Trocando para o Mapa 'Player'");
        InputActions.Player.Enable();
        InputActions.UI.Disable();
    }

    public void SwitchToUIMap()
    {
        Debug.LogWarning("INPUT MANAGER: Trocando para o Mapa 'UI'");
        InputActions.UI.Enable();
        InputActions.Player.Disable();
    }

    // Segurança (boa prática)
    private void OnDestroy()
    {
        // Desliga os mapas se o manager for destruído
        InputActions?.Player.Disable();
        InputActions?.UI.Disable();
    }
}