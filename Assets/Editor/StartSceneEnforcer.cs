#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class StartSceneEnforcer
{
    // ***** AJUSTE ESTA LINHA COM O CAMINHO CORRETO DA SUA CENA PRINCIPAL *****
    // Exemplo: Se sua cena de Menu/Intro está em "Assets/Scenes/IntroScene.unity"
    private const string StartScenePath = "Assets/Scenes/Boot.unity";

    // Nome do Item no Menu (Opcional, mas útil para o time)
    private const string MenuPath = "Tools/Game/Set Start Scene";

    // Variável para armazenar o Asset da cena
    private static SceneAsset startScene;

    // É chamado sempre que os scripts são carregados (ao iniciar o Unity ou compilar)
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        // Carrega o SceneAsset pelo caminho definido
        startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);

        if (startScene != null)
        {
            // Define a cena inicial usando a API oficial
            EditorSceneManager.playModeStartScene = startScene;
            Debug.Log($"[Game Dev] Cena de Início forçada para: {startScene.name}");
        }
        else
        {
            Debug.LogError($"[Game Dev] ERRO: Cena inicial não encontrada em '{StartScenePath}'. Verifique o caminho!");
        }
    }

    // Cria um item de menu para redefinir ou verificar a cena (Opcional)
    [MenuItem(MenuPath)]
    private static void SetStartSceneFromMenu()
    {
        Initialize();
    }
}
#endif