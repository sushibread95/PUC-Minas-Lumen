using UnityEngine;

public class LevelAudioStarter : MonoBehaviour
{
    [Header("Ambiente / Música")]
    [Tooltip("O áudio que vai tocar em loop nesta fase (ex: Vento, Música de Tensão)")]
    public AudioClip levelAmbience;
    
    [Range(0f, 1f)] public float volume = 0.5f;

    void Start()
    {
        if (AudioManager.Instance != null && levelAmbience != null)
        {
            // Toca a música/ambiente usando o canal de música do AudioManager
            AudioManager.Instance.PlayMusic(levelAmbience, volume);
        }
    }
}