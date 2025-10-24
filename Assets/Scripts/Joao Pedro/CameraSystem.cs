using UnityEngine;

public class CameraSystem : MonoBehaviour
{
    [Header("Components")]
	[SerializeField] private Camera mainCamera;
    [SerializeField] private Transform anchor;
    [Header("Position and view Atributes")]
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothSpeed;
    [SerializeField][Range(30, 90)] private float fieldOfView;
    [Header("Movement Atributes")]
    [SerializeField] private float sensitivity;
    public Vector2 turn;
    private void OnEnable()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }
    private void Update()
    {
        turn.x += Input.GetAxis("Mouse X") * sensitivity;
        turn.y += Input.GetAxis("Mouse Y") * sensitivity;
        anchor.localRotation = Quaternion.Euler(-turn.y, turn.x, 0f);
    }
    private void FixedUpdate()
    {
        anchor.position = Vector3.Lerp(anchor.position, this.transform.position + offset, smoothSpeed * Time.fixedDeltaTime);
    } 
}
