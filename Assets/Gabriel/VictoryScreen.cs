using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryScreen : MonoBehaviour
{
    [Header("Nome da Cena do Menu")]
    public string mainMenuSceneName = "MainMenu"; // Coloque o nome exato da sua cena de Menu

    void Start()
    {
        // --- OBRIGATÓRIO: DESTRAVA O MOUSE ---
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Opcional: Para qualquer som de batalha/ambiente que tenha sobrado
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
    }

    // Função para o Botão "Voltar ao Menu"
    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // Garante que o jogo não está pausado
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    // Opcional: Botão Sair
    public void QuitGame()
    {
        Application.Quit();
    }
}