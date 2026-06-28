using UnityEngine;
using UnityEngine.AI;

// Passos do inimigo por timer (sem editar animação). Enquanto o NavMeshAgent
// estiver se movendo, toca um som de passo em intervalos — espacial (3D), então
// soa de onde o inimigo está. A cadência acelera com a velocidade.
// Coloque este componente no inimigo (mesmo objeto do NavMeshAgent).
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyFootsteps : MonoBehaviour
{
    [Header("Sons de passo (sorteados)")]
    public AudioClip[] footstepSounds;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Cadência")]
    [Tooltip("Intervalo base entre passos (segundos) na velocidade normal do agente.")]
    public float stepInterval = 0.5f;
    [Tooltip("Velocidade mínima do agente para contar como 'andando'.")]
    public float minSpeedToStep = 0.3f;

    private NavMeshAgent agent;
    private float nextStepTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent == null || !agent.enabled)
            return;

        float speed = agent.velocity.magnitude;

        // Parado: não toca e adia o próximo passo (evita "spam" ao retomar).
        if (speed < minSpeedToStep)
        {
            nextStepTime = Time.time + stepInterval * 0.5f;
            return;
        }

        if (Time.time >= nextStepTime)
        {
            PlayStep();

            // Quanto mais rápido, mais curto o intervalo (corre = passos mais juntos).
            float reference = agent.speed > 0.01f ? agent.speed : speed;
            float speedFactor = Mathf.Clamp(speed / reference, 0.5f, 1.6f);
            nextStepTime = Time.time + stepInterval / speedFactor;
        }
    }

    private void PlayStep()
    {
        if (footstepSounds == null || footstepSounds.Length == 0 || AudioManager.Instance == null)
            return;

        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        if (clip != null)
            AudioManager.Instance.PlaySFX(clip, transform.position, volume);
    }
}
