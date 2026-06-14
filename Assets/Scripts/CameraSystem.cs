using UnityEngine;
using UnityEngine.InputSystem;

// CORREÇÃO: este script usava Input.GetAxis (Input Manager LEGADO). Com o
// projeto configurado para "Input System Package (New)", isso lança
// InvalidOperationException em runtime. Agora lê o mouse pelo Input System,
// mantendo o legado como fallback de compilação condicional.
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
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            // 0.05f aproxima a escala do antigo Input.GetAxis("Mouse X/Y")
            Vector2 delta = Mouse.current.delta.ReadValue() * 0.05f;
            turn.x += delta.x * sensitivity;
            turn.y += delta.y * sensitivity;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        turn.x += Input.GetAxis("Mouse X") * sensitivity;
        turn.y += Input.GetAxis("Mouse Y") * sensitivity;
#endif
        if (anchor != null)
            anchor.localRotation = Quaternion.Euler(-turn.y, turn.x, 0f);
    }
    private void FixedUpdate()
    {
        anchor.position = Vector3.Lerp(anchor.position, this.transform.position + offset, smoothSpeed * Time.fixedDeltaTime);
    } 
}
