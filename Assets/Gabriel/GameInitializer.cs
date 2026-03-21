using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameInitializer : MonoBehaviour
{
    public string mainMenuScene = "MainMenu";
    
    void Start()
    {
        StartCoroutine(LoadMainMenuAndUnloadBoot());
    }
    
    private IEnumerator LoadMainMenuAndUnloadBoot()
    {
        Debug.Log("Boot: Iniciando carregamento assíncrono do MainMenu...");

        //carregamento assíncrono da cena MainMenu
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainMenuScene, LoadSceneMode.Additive);
        
        // Proteção contra nomes de cena digitados errados no Inspector
        if (asyncLoad == null)
        {
            Debug.LogError($"Boot: Falha ao carregar a cena '{mainMenuScene}'. Ela está no Build Settings?");
            yield break; // Para a corotina imediatamente
        }

        // 2. Esperamos o carregamento  
        while (!asyncLoad.isDone)
        {
            yield return null; // Espera o próximo frame
        }

        Debug.Log("Boot: MainMenu carregado. Configurando cena ativa...");

        // 3. Define o MainMenu como cena ativa com segurança
        Scene mainMenuSceneRef = SceneManager.GetSceneByName(mainMenuScene);
        if (mainMenuSceneRef.IsValid())
        {
            SceneManager.SetActiveScene(mainMenuSceneRef);
        }
        else
        {
            Debug.LogError("Boot: Não foi possível encontrar a cena carregada para ativá-la.");
        }
        
        // 4. Descarta a cena Boot
        SceneManager.UnloadSceneAsync("Boot");
    }
}