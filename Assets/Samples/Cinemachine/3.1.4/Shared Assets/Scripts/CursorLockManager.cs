using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Cinemachine.Samples
{
    public class CursorLockManager : MonoBehaviour, Unity.Cinemachine.IInputAxisOwner
    {
        public InputAxis CursorLock = InputAxis.DefaultMomentary;

        public UnityEvent OnCursorLocked = new ();
        public UnityEvent OnCursorUnlocked = new ();

        bool m_IsTriggered;

        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            axes.Add(new()
            {
                DrivenAxis = () => ref CursorLock, Name = "CursorLock",
                Hint = IInputAxisOwner.AxisDescriptor.Hints.X
            });
        }

        void OnEnable() => UnlockCursor();
        void OnDisable() => UnlockCursor();

        void Update() // ou LateUpdate
        {   
            if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
        return; 
        }

        }
        public void LockCursor()
        {
            Debug.LogError($"!!! SCRIPT {this.GetType().Name} ESTÁ TENTANDO TRAVAR O CURSOR AGORA (Pausado={PauseMenuManager.Instance?.IsPaused}) !!!");
            if (enabled)
            {
                Cursor.lockState = CursorLockMode.Locked;
                OnCursorLocked.Invoke();
            }
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            OnCursorUnlocked.Invoke();
        }
    }
}
