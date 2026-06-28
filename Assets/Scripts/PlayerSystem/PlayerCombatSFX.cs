using UnityEngine;

// Sons de combate do player, dirigidos por eventos (sem tocar nada dentro do
// código de combate). Toca um clip ao ATACAR e ao TOMAR DANO. Use arrays para
// sortear variações e evitar repetição. Coloque este componente no Player.
public class PlayerCombatSFX : MonoBehaviour
{
    [Header("Ataque (OnPlayerAttacked)")]
    public AudioClip[] attackSounds;
    [Range(0f, 1f)] public float attackVolume = 1f;

    [Header("Tomar dano (OnPlayerDamaged)")]
    public AudioClip[] hurtSounds;
    [Range(0f, 1f)] public float hurtVolume = 1f;

    private void OnEnable()
    {
        PlayerControllerSystem.OnPlayerAttacked += HandleAttacked;
        HealthSystem.OnPlayerDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        PlayerControllerSystem.OnPlayerAttacked -= HandleAttacked;
        HealthSystem.OnPlayerDamaged -= HandleDamaged;
    }

    private void HandleAttacked() => PlayRandom(attackSounds, attackVolume);
    private void HandleDamaged() => PlayRandom(hurtSounds, hurtVolume);

    private void PlayRandom(AudioClip[] pool, float volume)
    {
        if (pool == null || pool.Length == 0 || AudioManager.Instance == null)
            return;

        AudioClip clip = pool[Random.Range(0, pool.Length)];
        if (clip != null)
            AudioManager.Instance.PlaySFX(clip, volume);
    }
}
