using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Canais de Áudio")]
    public AudioSource musicSource;
    public AudioSource uiSource;
    public AudioSource sfxSource;

    [Header("Volumes")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();
        if (!uiSource) uiSource = gameObject.AddComponent<AudioSource>();
        if (!sfxSource) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        uiSource.ignoreListenerPause = true; 
    }

    // --- MÚSICA (Agora aceita 1 ou 2 argumentos) ---
    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying)) return;
        musicSource.clip = clip;
        musicSource.volume = volume * masterVolume;
        musicSource.Play();
    }

    // --- SFX PADRÃO (Aceita volume opcional) ---
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume * masterVolume);
    }

    // --- SFX EM POSIÇÃO (Para resolver o erro das Portas e Player) ---
    // Adicionamos esta versão para o compilador não reclamar de 2 argumentos
    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        // Toca no canal de SFX mas você pode usar PlayClipAtPoint se quiser 3D estrito
        AudioSource.PlayClipAtPoint(clip, position, volume * masterVolume);
    }

    public void PlayUISFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        uiSource.PlayOneShot(clip, volume * masterVolume);
    }

    public void StopMusic() => musicSource.Stop();
}