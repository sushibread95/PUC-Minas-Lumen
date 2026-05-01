using System;
using System.Diagnostics;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [HideInInspector] public enum BillboardType { LookAtCamera, CameraForward };
    [SerializeField] private BillboardType billboardType;
    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }
    private void LateUpdate()
    {
        switch (billboardType)
        {
            case BillboardType.LookAtCamera:
                transform.LookAt(mainCamera.transform.position, Vector3.up);
                break;
            case BillboardType.CameraForward:
                transform.forward = mainCamera.transform.forward;
                break;
        }
    }
}
