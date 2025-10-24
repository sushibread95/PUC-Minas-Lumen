using UnityEditor.Experimental.GraphView;
using UnityEngine;
public class Movement : MonoBehaviour
{
	[Header("Components")]
	[SerializeField] private Camera mainCamera;
	public Rigidbody body;
 	[Header("Speed Atributes")]
	[SerializeField] private float acceleration;
	[SerializeField] private float maxSpeed;
	[Header("Jump Atributes")]
	[SerializeField] private KeyCode jumpKey = KeyCode.Space;
	[SerializeField] private float jumpForce;
	[SerializeField] private float verticalMaxSpeed;
	[SerializeField] private float height;
	[SerializeField] private LayerMask layerMask;
	[SerializeField] private float midAirMaxSpeed;
	[Header("Crouch Atributes")]
	[SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
	[SerializeField] private float crouchMaxSpeed;
	[Header("Sprinting Atributes")]
	[SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
	[SerializeField] private float sprintingAcceleration;
	[SerializeField] private float sprintingMaxSpeed;

	// Hidden values
	[HideInInspector] public bool canMove = true, canJump = true;
	[HideInInspector] public bool isMoving = false, isCrouching = false, isSprinting = false, isAirborne = false, isJumping = false;
	[HideInInspector] public float currentMaxSpeed;
	private Vector3 movementDirection;
	private void OnEnable()
	{
		if (mainCamera == null) mainCamera = Camera.main;
		currentMaxSpeed = maxSpeed;
	}
	private void Update()
	{
		isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
		isJumping = Input.GetKey(jumpKey);

		if (Input.GetKeyDown(crouchKey))
		{
			isCrouching = !isCrouching;
			isSprinting = false;
			if (isCrouching) currentMaxSpeed = crouchMaxSpeed;
		}

		if (Input.GetKeyDown(sprintKey))
		{
			isSprinting = !isSprinting;
			isCrouching = false;
		}

		Vector3 cameraForward = mainCamera.transform.forward;
		cameraForward.y = 0f;
		cameraForward.Normalize();

		Vector3 cameraRight = mainCamera.transform.right;
		cameraRight.y = 0f;
		cameraRight.Normalize();

		float horizontalInput = Input.GetAxis("Horizontal");
		float verticalInput = Input.GetAxis("Vertical");

		movementDirection = (cameraForward * verticalInput) + (cameraRight * horizontalInput);
		movementDirection.Normalize();
		//if (isMoving) movementDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

	}
	private void FixedUpdate()
	{
		RaycastHit hit;
		canJump = Physics.Raycast(this.transform.position + new Vector3(0f, 0.1f, 0f), Vector3.down, out hit, height + 0.1f, layerMask);
		if (canJump && isJumping) body.AddForce(jumpForce * Vector3.up, ForceMode.Force);

		if (canMove && isMoving)
		{
			Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
			body.rotation = Quaternion.Slerp(body.rotation, targetRotation, Time.fixedDeltaTime * 10f);
			body.AddForce(movementDirection * acceleration, ForceMode.Force);
		}

		Vector3 horizontalVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
		if (horizontalVelocity.magnitude > currentMaxSpeed && !isAirborne) body.AddForce(-horizontalVelocity.normalized * acceleration * 1.5f, ForceMode.Force);
		else if(horizontalVelocity.magnitude > midAirMaxSpeed) body.AddForce(-horizontalVelocity.normalized * midAirMaxSpeed * 1.5f, ForceMode.Force);

		float verticalVelocity = body.linearVelocity.y;
		if (isAirborne)
		{
			if (verticalVelocity < 0) body.AddForce(Vector3.down * acceleration * 1.5f, ForceMode.Force);
			else if (verticalVelocity > verticalMaxSpeed) body.AddForce(-new Vector3(0f, verticalVelocity, 0f).normalized * 1.5f, ForceMode.Force);
		}
	}
}
