using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    public string mainMenuScene = "MainMenu";
    
    void Start()
    {
        // Carrega o MainMenu e depois descarrega a Boot
        StartCoroutine(LoadMainMenuAndUnloadBoot());
    }
    
    private System.Collections.IEnumerator LoadMainMenuAndUnloadBoot()
    {
        // Carrega o MainMenu aditivamente
        SceneManager.LoadScene(mainMenuScene, LoadSceneMode.Additive);
        
        // Espera o carregamento
        yield return new WaitForSeconds(0.5f);
        
        // Define o MainMenu como cena ativa
        Scene mainMenuSceneRef = SceneManager.GetSceneByName(mainMenuScene);
        if (mainMenuSceneRef.IsValid())
        {
            SceneManager.SetActiveScene(mainMenuSceneRef);
        }
        
        // Opcional: Descarta a cena Boot
        SceneManager.UnloadSceneAsync("Boot");
    }
}