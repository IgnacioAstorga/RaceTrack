using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class VehicleController : MonoBehaviour {

	public Vector3 Velocity { get; private set; }

	public Transform body;

	public LayerMask raceTrackLayer;
	public float gravityRayMaxDistance = 10f;
	public float gravityReorientationSpeed = 360f;

	public float turnRate = 45f;
	public float acceleration = 10f;
	public float maxSpeed = 50f;

	private float _horizontalInput;
	private float _accelerateInput;

	private Vector3 _gravity;

	private CharacterController _characterController;

	void Awake() {

		// Retrieves the desired componets
		_characterController = GetComponent<CharacterController>();
	}

	void Update() {

		// Reads the user input
		ReadInput();
	}

	private void ReadInput() {

		// Reads the horizontal axis
		_horizontalInput = Input.GetAxis("Horizontal");

		// Reads the aaceleration input
		_accelerateInput = Input.GetAxis("Accelerate");
	}

	void FixedUpdate() {

		// Reorientates the vehicle to match the track's curvature
		MatchTrackCurvature();

		// Turns the vehicle matching the user input
		TurnVehicle();

		// Accelerates the vehicle towards it's forward direction
		Accelerate();

		// Finally, moves the vehicle using it's velocity
		Move();
	}

	private void MatchTrackCurvature() {

		// Casts a ray downwards to check for the track orientation
		RaycastHit trackHit;
		if (Physics.Raycast(body.position, -body.up, out trackHit, gravityRayMaxDistance, raceTrackLayer)) {

			// Uses the track orientation for the gravity
			float gravityMagnitude = Physics.gravity.magnitude;
			_gravity = -trackHit.SmoothedNormal() * gravityMagnitude;
		}
		else {

			// If the track was not hit, uses the default gravity
			_gravity = Physics.gravity;
		}

		// Orientates the object to match the gravity
		float normalAngle = Vector3.Angle(body.up, -_gravity);
		Vector3 normalAxis = Vector3.Cross(body.up, -_gravity);
		if (normalAngle > 0) {
			Quaternion targetRotation = Quaternion.AngleAxis(normalAngle, normalAxis) * body.rotation;
			body.rotation = Quaternion.RotateTowards(body.rotation, targetRotation, gravityReorientationSpeed * Time.deltaTime);
		}
	}

	private void TurnVehicle() {

		// Rotates the forward direction using the turn rate
		float amountTurned = turnRate * _horizontalInput * Time.deltaTime;
		amountTurned *= (1 - Velocity.magnitude / maxSpeed);
		Quaternion turnRotation = Quaternion.AngleAxis(amountTurned, body.up);
		body.rotation = turnRotation * body.rotation;
		Velocity = turnRotation * Velocity;
	}

	private void Accelerate() {

		// Accelerates the velocity using the vehicle's forward direction and user's input
		Velocity += _accelerateInput * acceleration * body.forward * Time.deltaTime;

		// Adds the gravity to the velocity
		Velocity += _gravity * Time.deltaTime;

		// Applies drag to the velocity
		float drag = Acceleration.GetDragFromAcceleration(acceleration, maxSpeed);
		Velocity *= Mathf.Clamp01(1f - drag * Time.fixedDeltaTime);
	}

	private void Move() {

		// Moves the vehicle using the character's controlelr
		_characterController.Move(Velocity * Time.deltaTime);
	}

	void OnControllerColliderHit(ControllerColliderHit hit) {

		// In order to get a smooth normal, we need more information
		Vector3 normal = hit.normal;
		RaycastHit colliderHit;
		if (hit.collider.Raycast(new Ray(hit.point + hit.normal, -hit.normal), out colliderHit, 2 * hit.moveLength))
			normal = colliderHit.SmoothedNormal();

		// Projects the velocity along the hit surface
		Velocity = Vector3.ProjectOnPlane(Velocity, normal);
	}
}