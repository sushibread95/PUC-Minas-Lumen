using UnityEngine;

public class LevelAudioStarter : MonoBehaviour
{
    [Header("Ambiente / Música")]
    [Tooltip("O áudio que vai tocar em loop nesta fase. Se quiser SILÊNCIO, deixe vazio e marque a opção abaixo.")]
    public AudioClip levelAmbience;
    
    [Tooltip("Marque isto se quiser que qualquer música da cena anterior seja parada ao entrar aqui.")]
    public bool forceStopMusic = false;

    void Start()
    {
        if (AudioManager.Instance == null) return;

        // 1. Se a cena pede silêncio, desliga o rádio.
        if (forceStopMusic)
        {
            AudioManager.Instance.StopMusic();
            return; // Encerra a função aqui
        }

        // 2. Se tem uma música para tocar, troca a estação do rádio.
        // (Removi o parâmetro 'volume' para alinhar com o nosso novo AudioManager blindado)
        if (levelAmbience != null)
        {
            AudioManager.Instance.PlayMusic(levelAmbience);
        }
    }
}