using System;
using System.Reflection;
using UnityEngine;

public class EnemyDeathVisualCleanup : MonoBehaviour
{
    #region Inspector - References

    [Header("REFERÊNCIAS")]
    [Tooltip("Objeto visual principal do inimigo. Se vazio, usa este GameObject.")]
    [SerializeField] private GameObject modelRoot;

    [Tooltip("Componente de vida do inimigo. Pode deixar vazio para auto busca por EnemyHealth ou HealthSystem.")]
    [SerializeField] private Component healthComponent;

    [Tooltip("Alvo de lock-on deste inimigo. Será desativado ao morrer.")]
    [SerializeField] private LockOnTarget lockOnTarget;

    #endregion

    #region Inspector - Death Visual

    [Header("VISUAL DE MORTE")]
    [Tooltip("Modelo destruído que será instanciado na posição do inimigo.")]
    [SerializeField] private GameObject destroyedModelPrefab;

    [Tooltip("Efeito de fumaça/partícula que será instanciado na posição do inimigo.")]
    [SerializeField] private GameObject deathVfxPrefab;

    [Tooltip("Ponto onde o modelo destruído e o VFX aparecem. Se vazio, usa a posição do inimigo.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Desliga o modelo original do inimigo ao morrer.")]
    [SerializeField] private bool hideOriginalModel = true;

    [Tooltip("Desativa colliders do inimigo ao morrer para limpar colisões.")]
    [SerializeField] private bool disableColliders = true;

    [Tooltip("Desativa scripts comuns do inimigo ao morrer. Não desativa este script.")]
    [SerializeField] private bool disableEnemyBehaviours = true;

    [Tooltip("Destrói o objeto raiz do inimigo após o tempo definido. Use 0 para não destruir.")]
    [SerializeField] private float destroyRootAfter = 2.5f;

    [Tooltip("Destrói automaticamente o VFX instanciado após este tempo. Use 0 para deixar na cena.")]
    [SerializeField] private float destroyVfxAfter = 4f;

    #endregion

    #region Inspector - Audio And Events

    [Header("ÁUDIO E EVENTOS")]
    [SerializeField] private AudioClip deathSound;

    [Tooltip("ID usado em GameEvents.TriggerEnemyDeath. Deixe vazio se outro script já dispara a quest.")]
    [SerializeField] private string enemyID;

    [Tooltip("Ative apenas se este script deve notificar quests. Se EnemyHealth já faz isso, deixe desligado.")]
    [SerializeField] private bool triggerGameEventOnCleanup = false;

    [Tooltip("Se ligado, o script tenta detectar automaticamente campos como isDead no EnemyHealth.")]
    [SerializeField] private bool autoDetectDeath = true;

    [SerializeField] private float deathCheckInterval = 0.1f;

    #endregion

    #region Runtime

    private bool cleanupPlayed;
    private float nextDeathCheckTime;

    #endregion

    #region Unity Lifecycle

    private void Reset()
    {
        modelRoot = gameObject;
        spawnPoint = transform;
        lockOnTarget = GetComponentInChildren<LockOnTarget>();
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void Update()
    {
        if (!autoDetectDeath || cleanupPlayed || Time.time < nextDeathCheckTime)
            return;

        nextDeathCheckTime = Time.time + Mathf.Max(0.02f, deathCheckInterval);

        if (IsHealthComponentDead())
            PlayDeathCleanup();
    }

    #endregion

    #region Setup

    // Busca referências sem obrigar mudanças no EnemyHealth atual.
    private void CacheReferences()
    {
        if (modelRoot == null)
            modelRoot = gameObject;

        if (spawnPoint == null)
            spawnPoint = transform;

        if (lockOnTarget == null)
            lockOnTarget = GetComponentInChildren<LockOnTarget>();

        if (healthComponent == null)
            healthComponent = FindHealthComponent();
    }

    private Component FindHealthComponent()
    {
        Component[] components = GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            string typeName = component.GetType().Name;
            if (typeName == "EnemyHealth" || typeName == "HealthSystem")
                return component;
        }

        return null;
    }

    #endregion

    #region Public API

    // Pode ser chamado diretamente pelo EnemyHealth quando a vida chegar a zero.
    public void PlayDeathCleanup()
    {
        if (cleanupPlayed)
            return;

        cleanupPlayed = true;
        CacheReferences();

        if (lockOnTarget != null)
            lockOnTarget.SetTargetable(false);

        SpawnReplacementVisuals();
        PlayDeathSound();
        NotifyEnemyDeathIfNeeded();
        DisableOriginalEnemy();
    }

    #endregion

    #region Death Detection

    // Lê campos/propriedades comuns de morte sem acoplar este script a uma classe específica.
    private bool IsHealthComponentDead()
    {
        if (healthComponent == null)
            return false;

        object owner = healthComponent;
        Type type = owner.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (TryReadBool(type, owner, flags, "isDead", out bool isDead))
            return isDead;

        if (TryReadBool(type, owner, flags, "IsDead", out isDead))
            return isDead;

        if (TryReadFloat(type, owner, flags, "currentHealth", out float currentHealth))
            return currentHealth <= 0f;

        if (TryReadFloat(type, owner, flags, "CurrentHealth", out currentHealth))
            return currentHealth <= 0f;

        if (TryReadFloat(type, owner, flags, "health", out currentHealth))
            return currentHealth <= 0f;

        return false;
    }

    private bool TryReadBool(Type type, object owner, BindingFlags flags, string name, out bool value)
    {
        value = false;
        FieldInfo field = type.GetField(name, flags);
        if (field != null && field.FieldType == typeof(bool))
        {
            value = (bool)field.GetValue(owner);
            return true;
        }

        PropertyInfo property = type.GetProperty(name, flags);
        if (property != null && property.CanRead && property.PropertyType == typeof(bool))
        {
            value = (bool)property.GetValue(owner);
            return true;
        }

        return false;
    }

    private bool TryReadFloat(Type type, object owner, BindingFlags flags, string name, out float value)
    {
        value = 0f;
        FieldInfo field = type.GetField(name, flags);
        if (field != null)
        {
            try
            {
                value = Convert.ToSingle(field.GetValue(owner));
                return true;
            }
            catch { return false; }
        }

        PropertyInfo property = type.GetProperty(name, flags);
        if (property != null && property.CanRead)
        {
            try
            {
                value = Convert.ToSingle(property.GetValue(owner));
                return true;
            }
            catch { return false; }
        }

        return false;
    }

    #endregion

    #region Cleanup

    // Cria modelo destruído e/ou fumaça no ponto da morte.
    private void SpawnReplacementVisuals()
    {
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        if (destroyedModelPrefab != null)
            Instantiate(destroyedModelPrefab, position, rotation);

        if (deathVfxPrefab != null)
        {
            GameObject vfx = Instantiate(deathVfxPrefab, position, rotation);
            if (destroyVfxAfter > 0f)
                Destroy(vfx, destroyVfxAfter);
        }
    }

    private void PlayDeathSound()
    {
        if (deathSound == null)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(deathSound, transform.position);
        else
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
    }

    private void NotifyEnemyDeathIfNeeded()
    {
        if (!triggerGameEventOnCleanup || string.IsNullOrEmpty(enemyID))
            return;

        GameEvents.TriggerEnemyDeath(enemyID);
    }

    private void DisableOriginalEnemy()
    {
        if (disableColliders)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        if (disableEnemyBehaviours)
        {
            Behaviour[] behaviours = GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this)
                    continue;

                if (behaviour is Animator)
                    continue;

                behaviour.enabled = false;
            }
        }

        if (hideOriginalModel && modelRoot != null)
            modelRoot.SetActive(false);

        if (destroyRootAfter > 0f)
            Destroy(gameObject, destroyRootAfter);
    }

    #endregion
}
