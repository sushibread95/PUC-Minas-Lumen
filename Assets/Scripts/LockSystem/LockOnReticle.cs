using UnityEngine;
using UnityEngine.UI;

public class LockOnReticle : MonoBehaviour
{
    private enum ReticleMode
    {
        TargetAimPoint,
        ScreenCenterWhileLocked
    }

    #region Inspector - References

    [Header("REFERÊNCIAS")]
    [SerializeField] private Camera cam;
    [SerializeField] private LockOnSystem lockOn;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Image img;

    #endregion

    #region Inspector - Display

    [Header("RETÍCULA")]
    [Tooltip("Target Aim Point prende a retícula no objeto vazio do inimigo. Screen Center deixa a mira fixa no centro.")]
    [SerializeField] private ReticleMode reticleMode = ReticleMode.TargetAimPoint;

    [Tooltip("Offset final da retícula. Use 0,0 para ficar exatamente no AimPoint ou no centro.")]
    [SerializeField] private Vector2 screenOffset = Vector2.zero;

    [Tooltip("Suavização da retícula. Use 0 para resposta instantânea.")]
    [SerializeField] private float positionSmooth = 20f;

    [Tooltip("Esconde a retícula se o alvo estiver atrás da câmera ou fora da tela.")]
    [SerializeField] private bool hideWhenTargetOffScreen = true;

    #endregion

    #region Runtime

    private RectTransform imgRect;
    private Vector2 currentAnchoredPosition;
    private bool hasPosition;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        CacheReferences();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        CacheReferences();
        UpdateReticlePosition();
    }

    #endregion

    #region Setup

    // Busca referências necessárias para posicionar a retícula corretamente no Canvas.
    private void CacheReferences()
    {
        if (img == null)
            img = GetComponent<Image>();

        if (imgRect == null && img != null)
            imgRect = img.rectTransform;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvasRect == null && canvas != null)
            canvasRect = canvas.transform as RectTransform;

        if (cam == null)
            cam = Camera.main;

        if (lockOn == null)
            lockOn = FindAnyObjectByType<LockOnSystem>();
    }

    #endregion

    #region Reticle Logic

    // Atualiza a retícula no AimPoint do inimigo ou no centro da tela.
    private void UpdateReticlePosition()
    {
        if (img == null || imgRect == null || cam == null || lockOn == null || canvasRect == null)
            return;

        if (!lockOn.IsLockedOn || lockOn.CurrentAimPoint == null)
        {
            SetVisible(false);
            hasPosition = false;
            return;
        }

        Vector3 targetScreenPoint = cam.WorldToScreenPoint(lockOn.CurrentAimPoint.position);

        if (ShouldHideForTargetScreenPoint(targetScreenPoint))
        {
            SetVisible(false);
            return;
        }

        Vector2 desiredPosition = reticleMode == ReticleMode.ScreenCenterWhileLocked
            ? GetCanvasLocalPoint(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f))
            : GetCanvasLocalPoint(targetScreenPoint);

        desiredPosition += screenOffset;

        if (!hasPosition || positionSmooth <= 0f)
        {
            currentAnchoredPosition = desiredPosition;
            hasPosition = true;
        }
        else
        {
            float t = 1f - Mathf.Exp(-positionSmooth * Time.deltaTime);
            currentAnchoredPosition = Vector2.Lerp(currentAnchoredPosition, desiredPosition, t);
        }

        imgRect.anchoredPosition = currentAnchoredPosition;
        SetVisible(true);
    }

    private bool ShouldHideForTargetScreenPoint(Vector3 screenPoint)
    {
        if (!hideWhenTargetOffScreen)
            return false;

        if (screenPoint.z <= 0f)
            return true;

        return screenPoint.x < 0f || screenPoint.x > Screen.width ||
               screenPoint.y < 0f || screenPoint.y > Screen.height;
    }

    private Vector2 GetCanvasLocalPoint(Vector2 screenPoint)
    {
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera != null ? canvas.worldCamera : cam;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, eventCamera, out Vector2 localPoint))
            return localPoint;

        return Vector2.zero;
    }

    private void SetVisible(bool visible)
    {
        if (img != null && img.enabled != visible)
            img.enabled = visible;
    }

    #endregion
}
