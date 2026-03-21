using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class DebugConsole : MonoBehaviour
{
    public static DebugConsole Instance;

    [Header("UI References (Painel)")]
    public GameObject debugPanel;

    [Header("Display de Status (Textos)")]
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI defText;

    [Header("Listas (Scroll View Content)")]
    public Transform itemsContainer;
    public Transform enemiesContainer;

    [Header("Assets")]
    public GameObject simpleButtonPrefab;

    private bool isOpen = false;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        debugPanel.SetActive(false);
    }

    void Start()
    {
        PopulateItemList();
        PopulateEnemyList();
    }

    void Update()
    {
        // Atalho F12
        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            ToggleDebug();
        }

        // Atualiza os textos em tempo real se o painel estiver aberto
        if (isOpen) UpdateStatDisplay();
    }

    public void ToggleDebug()
    {
        isOpen = !isOpen;
        debugPanel.SetActive(isOpen);

        if (isOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            UpdateStatDisplay();

            if (InputManager.Instance != null) InputManager.Instance.SwitchToUIMap();
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
        }
    }

    // --- FUN��ES DE BOT�O (MODIFICADORES) ---
    // Arraste estas fun��es para os bot�es [+] e [-] na Unity

    public void ModifyLevel(int amount)
    {
        if (PlayerStats.Instance == null) return;

        PlayerStats.Instance.level += amount;
        if (PlayerStats.Instance.level < 1) PlayerStats.Instance.level = 1; // M�nimo lv 1

        if (LevelingSystem.Instance != null)
            LevelingSystem.Instance.currentLevel = PlayerStats.Instance.level;

        UpdateStatDisplay();
    }

    public void ModifyHealth(float amount)
    {
        if (PlayerStats.Instance == null) return;
        PlayerStats.Instance.maxHealth += amount;

        // Atualiza o HealthSystem tamb�m
        if (HealthSystem.Instance != null)
        {
            HealthSystem.Instance.UpdateMaxStats(PlayerStats.Instance.maxHealth, PlayerStats.Instance.maxMana);
            // Opcional: Cura o valor adicionado
            if (amount > 0) HealthSystem.Instance.RecoverHealth(amount);
        }
        UpdateStatDisplay();
    }

    public void ModifyMana(float amount)
    {
        if (PlayerStats.Instance == null) return;
        PlayerStats.Instance.maxMana += amount;

        if (HealthSystem.Instance != null)
        {
            HealthSystem.Instance.UpdateMaxStats(PlayerStats.Instance.maxHealth, PlayerStats.Instance.maxMana);
            if (amount > 0) HealthSystem.Instance.RestoreMana(amount);
        }
        UpdateStatDisplay();
    }

    public void ModifyAttack(float amount)
    {
        if (PlayerStats.Instance == null) return;
        PlayerStats.Instance.physicalAttack += amount;
        UpdateStatDisplay();
    }

    public void ModifyDefense(float amount)
    {
        if (PlayerStats.Instance == null) return;
        PlayerStats.Instance.defense += amount;
        UpdateStatDisplay();
    }

    // --- VISUAL ---

    private void UpdateStatDisplay()
    {
        if (PlayerStats.Instance == null) return;

        if (levelText) levelText.text = PlayerStats.Instance.level.ToString();
        if (healthText) healthText.text = PlayerStats.Instance.maxHealth.ToString("F0"); // Sem casas decimais
        if (manaText) manaText.text = PlayerStats.Instance.maxMana.ToString("F0");
        if (atkText) atkText.text = PlayerStats.Instance.physicalAttack.ToString("F0");
        if (defText) defText.text = PlayerStats.Instance.defense.ToString("F0");
    }

    // --- LISTAS (IGUAL ANTES) ---

    private void PopulateItemList()
    {
        foreach (Transform child in itemsContainer) Destroy(child.gameObject);
        Objects[] allItems = Resources.LoadAll<Objects>("Items");

        foreach (Objects item in allItems)
        {
            GameObject btnObj = Instantiate(simpleButtonPrefab, itemsContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = item.objectName;
            btnObj.GetComponent<Button>().onClick.AddListener(() => GiveItem(item));
        }
    }

    private void GiveItem(Objects item)
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.AddItem(item);
    }

    private void PopulateEnemyList()
    {
        foreach (Transform child in enemiesContainer) Destroy(child.gameObject);
        GameObject[] allEnemies = Resources.LoadAll<GameObject>("Enemies");

        foreach (GameObject enemyPrefab in allEnemies)
        {
            GameObject btnObj = Instantiate(simpleButtonPrefab, enemiesContainer);
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = "Spawn " + enemyPrefab.name;
            btnObj.GetComponent<Button>().onClick.AddListener(() => SpawnEnemy(enemyPrefab));
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        PlayerControllerSystem player = Object.FindAnyObjectByType<PlayerControllerSystem>();
        if (player != null)
        {
            Vector3 spawnPos = player.transform.position + player.transform.forward * 4f;
            if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
                spawnPos = hit.point;

            Instantiate(prefab, spawnPos, Quaternion.LookRotation(player.transform.position - spawnPos));
            ToggleDebug();
        }
    }
}