using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour {

	public LayerMask raceTrackLayer;
	public float gravityRayMaxDistance = 10f;
	public float gravityReorientationSpeed = 360f;

	public float hoverDistance = 1f;
	public float hoverForce = 50f;

	public float turnRate = 45f;
	public float acceleration = 10f;
	public float maxSpeed = 50f;
	public float brakeStrength = 5f;

	private float _horizontalInput;
	private float _accelerateInput;
	private float _brakeInput;
	
	private Vector3 _gravity;

	private Rigidbody _rigidbody;
	private Transform _transform;

	void Awake() {

		// Retrieves the desired componets
		_rigidbody = GetComponent<Rigidbody>();
		_transform = transform;
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

		// Reads the aaceleration input
		_brakeInput = Input.GetAxis("Brake");
	}

	void FixedUpdate() {

		// Reorientates the vehicle to match the track's curvature
		MatchTrackCurvature();

		// Turns the vehicle matching the user input
		TurnVehicle();

		// Accelerates the vehicle towards it's forward direction
		Accelerate();
	}

	private void MatchTrackCurvature() {

		// Calculates the gravity from the track's curvature
		CalculateGravity();

		// Orientates the vehicle to match the gravity
		OrientateToGravity();

		// Makes the vehicle hover
		HoverOverTrack();
	}

	private void CalculateGravity() {

		// Casts a ray downwards to check for the track orientation
		RaycastHit trackHit;
		if (Physics.Raycast(_transform.position, -_transform.up, out trackHit, gravityRayMaxDistance, raceTrackLayer)) {

			// Uses the track orientation for the gravity
			float gravityMagnitude = Physics.gravity.magnitude;
			_gravity = -trackHit.SmoothedNormal() * gravityMagnitude;
		}
		else {

			// If the track was not hit, uses the default gravity
			_gravity = Physics.gravity;
		}
	}

	private void OrientateToGravity() {

		// Orientates the vehicle to match the gravity
		Vector3 projectedForward = Vector3.ProjectOnPlane(_transform.forward, -_gravity);
		Quaternion targetRotation = Quaternion.LookRotation(projectedForward, -_gravity);
		_transform.rotation = Quaternion.RotateTowards(_transform.rotation, targetRotation, gravityReorientationSpeed * Time.deltaTime);
	}

	private void HoverOverTrack() {

		// Adds force to the vehicle to separate it from the track
		RaycastHit trackHit;
		if (Physics.Raycast(_transform.position, -_transform.up, out trackHit, hoverDistance, raceTrackLayer)) {

			// Fist, projects the velocity into the track's curvature
			Vector3 projectedVelocity = Vector3.ProjectOnPlane(_rigidbody.velocity, trackHit.SmoothedNormal());
			_rigidbody.velocity = projectedVelocity.normalized * _rigidbody.velocity.magnitude;

			// The amount of force added is proportional to how close the vehicle is to the ground
			float proportionalDistance = 1f - trackHit.distance / hoverDistance;
			_rigidbody.AddForce(_transform.up * proportionalDistance * hoverForce);
		}
	}

	private void TurnVehicle() {

		// Rotates the forward direction using the turn rate
		float amountTurned = turnRate * _horizontalInput * Time.deltaTime;
		amountTurned *= (1 - _rigidbody.velocity.magnitude / maxSpeed);
		Quaternion turnRotation = Quaternion.AngleAxis(amountTurned, _transform.up);
		_transform.rotation = turnRotation * _transform.rotation;
		_rigidbody.velocity = turnRotation * _rigidbody.velocity;
	}

	private void Accelerate() {

		// Accelerates the velocity using the vehicle's forward direction and user's input
		_rigidbody.velocity += _accelerateInput * acceleration * _transform.forward * Time.deltaTime;

		// Adds the gravity to the velocity
		_rigidbody.velocity += _gravity * Time.deltaTime;

		// Applies drag to the velocity
		float drag = Acceleration.GetDragFromAcceleration(acceleration, maxSpeed);
		_rigidbody.velocity *= Mathf.Clamp01(1f - (1f + _brakeInput * brakeStrength) * drag * Time.fixedDeltaTime);
	}
}