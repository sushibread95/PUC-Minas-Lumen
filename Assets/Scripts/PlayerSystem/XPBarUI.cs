using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Feedback visual do XP. Lê os eventos do LevelingSystem e atualiza:
//  - duas barras (combate = vermelha, purificação = verde), preenchidas pela
//    contribuição de cada caminho ao NÍVEL ATUAL;
//  - o texto do nível;
//  - um texto flutuante "+X XP" a cada ganho (opcional);
//  - um flash de LEVEL UP (opcional).
//
// Todas as referências são opcionais: o que estiver vazio é simplesmente ignorado,
// então dá pra montar a UI aos poucos.
public class XPBarUI : MonoBehaviour
{
    [Header("Barras (Image com Image Type = Filled)")]
    [Tooltip("Barra de XP de combate (vermelha). Image com Image Type = Filled, Fill Method = Horizontal.")]
    public Image combatFill;
    [Tooltip("Barra de XP de purificação (verde). Image com Image Type = Filled, Fill Method = Horizontal.")]
    public Image purificationFill;

    [Header("Nível (opcional)")]
    public TMP_Text levelText;
    public string levelFormat = "Nível {0}";

    [Header("Texto flutuante de +XP (opcional)")]
    [Tooltip("Um TMP_Text DESATIVADO usado como molde. É clonado a cada ganho de XP.")]
    public TMP_Text floatingTextTemplate;
    public float floatingRise = 40f;
    public float floatingDuration = 1f;
    public Color combatColor = new Color(0.90f, 0.25f, 0.25f);
    public Color purificationColor = new Color(0.30f, 0.90f, 0.40f);
    public Color questColor = new Color(0.60f, 0.70f, 1.00f);

    [Header("Flash de Level Up (opcional)")]
    [Tooltip("Objeto ativado por alguns segundos ao subir de nível (ex.: um label 'LEVEL UP!').")]
    public GameObject levelUpFlash;
    public float levelUpFlashDuration = 1.5f;

    [Header("Suavização")]
    [Tooltip("Velocidade com que as barras deslizam até o valor alvo. 0 = instantâneo.")]
    public float fillLerpSpeed = 8f;

    private float targetCombatFill;
    private float targetPurifyFill;
    private bool subscribed;

    private void OnEnable()
    {
        if (floatingTextTemplate != null) floatingTextTemplate.gameObject.SetActive(false);
        if (levelUpFlash != null) levelUpFlash.SetActive(false);
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && LevelingSystem.Instance != null)
        {
            LevelingSystem.Instance.OnStatsChanged -= Refresh;
            LevelingSystem.Instance.OnXPGained -= HandleXPGained;
            LevelingSystem.Instance.OnLevelUp -= HandleLevelUp;
        }
        subscribed = false;
    }

    private void Update()
    {
        // O LevelingSystem vive na Boot (DontDestroyOnLoad); se a UI nasceu antes
        // dele estar pronto, reassina assim que ele aparecer.
        if (!subscribed)
            TrySubscribe();

        // Desliza as barras suavemente até o alvo.
        if (combatFill != null)
            combatFill.fillAmount = fillLerpSpeed > 0f
                ? Mathf.Lerp(combatFill.fillAmount, targetCombatFill, Time.unscaledDeltaTime * fillLerpSpeed)
                : targetCombatFill;

        if (purificationFill != null)
            purificationFill.fillAmount = fillLerpSpeed > 0f
                ? Mathf.Lerp(purificationFill.fillAmount, targetPurifyFill, Time.unscaledDeltaTime * fillLerpSpeed)
                : targetPurifyFill;
    }

    private void TrySubscribe()
    {
        if (subscribed || LevelingSystem.Instance == null) return;

        LevelingSystem.Instance.OnStatsChanged += Refresh;
        LevelingSystem.Instance.OnXPGained += HandleXPGained;
        LevelingSystem.Instance.OnLevelUp += HandleLevelUp;
        subscribed = true;

        Refresh();
    }

    // Recalcula os alvos das barras e o texto do nível.
    private void Refresh()
    {
        LevelingSystem ls = LevelingSystem.Instance;
        if (ls == null) return;

        float denom = Mathf.Max(1f, ls.xpToNextLevel);
        targetCombatFill = Mathf.Clamp01(ls.combatXPThisLevel / denom);
        targetPurifyFill = Mathf.Clamp01(ls.purificationXPThisLevel / denom);

        if (levelText != null)
            levelText.text = string.Format(levelFormat, ls.currentLevel);
    }

    // Cria um "+X XP" flutuante colorido conforme a fonte.
    private void HandleXPGained(float amount, XPType type)
    {
        if (floatingTextTemplate == null || amount <= 0f) return;

        TMP_Text clone = Instantiate(floatingTextTemplate, floatingTextTemplate.transform.parent);
        clone.gameObject.SetActive(true);
        clone.text = $"+{Mathf.RoundToInt(amount)} XP";
        clone.color = type == XPType.Combat ? combatColor
                    : type == XPType.Purification ? purificationColor
                    : questColor;

        StartCoroutine(FloatAndFade(clone));
    }

    private IEnumerator FloatAndFade(TMP_Text text)
    {
        RectTransform rt = text.rectTransform;
        Vector2 start = rt.anchoredPosition;
        Color baseColor = text.color;
        float time = 0f;

        while (time < floatingDuration)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(time / floatingDuration);

            rt.anchoredPosition = start + Vector2.up * (floatingRise * k);
            Color c = baseColor; c.a = 1f - k; text.color = c;

            yield return null;
        }

        Destroy(text.gameObject);
    }

    private void HandleLevelUp(int newLevel)
    {
        Refresh();

        if (levelUpFlash != null)
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        levelUpFlash.SetActive(true);
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, levelUpFlashDuration));
        levelUpFlash.SetActive(false);
    }
}
