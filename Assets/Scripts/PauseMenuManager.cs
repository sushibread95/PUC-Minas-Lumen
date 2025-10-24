using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Panel (CanvasGroup)")]
    public CanvasGroup pausePanel;    

    [Header("Buttons (optional)")]
    public Button resumeButton;
    public Button restartButton;
    public Button quitButton;

    [Header("Behavior")]
    public bool lockCursorInGameplay = true; 
    public bool selectFirstButtonOnOpen = true; 

    PlayerInputActions input;
    bool paused;

    void Awake()
    {
        input = new PlayerInputActions();
        input.Enable();
        input.Player.Pause.performed += OnPausePerformed;

        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (restartButton) restartButton.onClick.AddListener(RestartScene);
        if (quitButton) quitButton.onClick.AddListener(QuitGame);
    }

    void OnDestroy()
    {
        input.Player.Pause.performed -= OnPausePerformed;
        input.Disable();
    }

    void Start()
    {
        Show(false);           
        EnsureTimescale(1f);   
        SetCursorLocked(true); 
    }

    void OnPausePerformed(InputAction.CallbackContext _)
    {
        if (paused) Resume(); else Pause();
    }

    public void Pause()
    {
        if (paused) return;
        paused = true;
        Show(true);
        EnsureTimescale(0f);
        SetCursorLocked(false);

        if (selectFirstButtonOnOpen && resumeButton)
        {
            EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
        }
    }

    public void Resume()
    {
        if (!paused) return;
        paused = false;
        Show(false);
        EnsureTimescale(1f);
        SetCursorLocked(lockCursorInGameplay);
    }

    public void RestartScene()
    {
        EnsureTimescale(1f);
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    void Show(bool visible)
    {
        if (!pausePanel) return;
        pausePanel.alpha = visible ? 1f : 0f;
        pausePanel.interactable = visible;
        pausePanel.blocksRaycasts = visible;
        if (!pausePanel.gameObject.activeSelf) pausePanel.gameObject.SetActive(true);
    }

    void EnsureTimescale(float value)
    {
        Time.timeScale = value;
        AudioListener.pause = (value == 0f);
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
