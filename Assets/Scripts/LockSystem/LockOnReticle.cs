using UnityEngine;
using UnityEngine.UI;

public class LockOnReticle : MonoBehaviour
{
    public Camera cam;
    public LockOnSystem lockOn;
    public Image img;                  // optional, auto-get
    public Vector2 screenOffset = new Vector2(0, 0); // tweak if needed

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!img) img = GetComponent<Image>();
    }

    void LateUpdate()
    {
        if (!lockOn || !cam || !img) return;

        if (!lockOn.IsLockedOn)
        {
            if (img.enabled) img.enabled = false;
            return;
        }

        Transform aim = lockOn.CurrentAimPoint;
        if (!aim)
        {
            if (img.enabled) img.enabled = false;
            return;
        }

        Vector3 sp = cam.WorldToScreenPoint(aim.position);
        bool onScreen = sp.z > 0f;
        if (!onScreen)
        {
            if (img.enabled) img.enabled = false;
            return;
        }

        if (!img.enabled) img.enabled = true;
        img.rectTransform.anchoredPosition = (Vector2)sp + screenOffset - new Vector2(Screen.width, Screen.height) * 0.5f;
    }
}
