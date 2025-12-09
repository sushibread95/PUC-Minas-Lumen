using UnityEngine;

public class UIBillboard : MonoBehaviour
{
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            return;
        }
        transform.rotation = mainCam.transform.rotation;
    }
}