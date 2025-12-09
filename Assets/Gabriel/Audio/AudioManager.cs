using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Componentes")]
    [Tooltip("AudioSource para tocar MÚSICA de fundo. Deve estar na raiz deste GameObject.")]
    public AudioSource musicSource;
    [Tooltip("Volume MESTRE. Ajusta a saída geral do sistema.")]
    [Range(0f, 1f)] public float masterVolume = 0.8f;

    void Awake()
    {
        // Lógica de Singleton Persistente (igual aos outros Managers)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // Garante que o AudioSource para música exista
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.loop = true; // Música geralmente faz loop
    }

    // --- FUNÇÕES PÚBLICAS ---

    // 1. Para Música (Longos e persistentes)
    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = volume * masterVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // 2. Para SFX (Curto e em 3D)
    // Usamos PlayClipAtPoint para instanciar o som no mundo (simples, mas eficaz)
    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume * masterVolume);
    }
    
    // 3. Para SFX de UI (Sem posição no mundo)
    public void PlayUISFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        // Usa a posição da câmera principal para simular som 2D
        if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume * masterVolume);
        }
    }
}