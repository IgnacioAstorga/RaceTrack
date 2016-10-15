using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour {

	public bool Grounded { get; private set; }

	public Transform model;

	public Transform[] hoverPoints;
	public LayerMask trackGravityLayer;
	public float gravityRayMaxDistance = 10f;
	public float gravityReorientationSpeed = 5f;

	public LayerMask trackCollisionLayer;
	public float hoverDistance = 1f;
	public float hoverForce = 50f;
	public float tiltAngle = 15f;
	public float tiltSpeed = 15f;

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

	void Start() {

		// Checks if everything is OK
		if (model == null) {
			Debug.LogError("ERROR: Missing reference to the model object!");
			enabled = false;
			return;
		}
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

		// Calculates the gravity from the track's curvature
		CalculateGravity();

		// Makes the vehicle hover, orientating it to match the track
		HoverOverTrack();

		// Turns the vehicle matching the user input
		TurnVehicle();

		// Tilts the vehicle's model using the turn speed
		TiltModel();

		// Accelerates the vehicle towards it's forward direction
		Accelerate();
	}

	private void CalculateGravity() {

		// Casts a ray downwards to check for the track orientation
		RaycastHit trackHit;
		Vector3 targetGravity = Physics.gravity;
		if (Physics.Raycast(_transform.position, -_transform.up, out trackHit, gravityRayMaxDistance, trackGravityLayer)) {

			// Uses the track orientation for the gravity
			float gravityMagnitude = Physics.gravity.magnitude;
			targetGravity = -trackHit.SmoothedNormal() * gravityMagnitude;
		}

		// Lerps the gravity to it's target value
		_gravity = Vector3.Lerp(_gravity, targetGravity, gravityReorientationSpeed * Time.deltaTime);
	}

	private void HoverOverTrack() {

		// Checks the average normal of the hover points
		Vector3 hoverNormal = Vector3.zero;
		int rayHitCount = 0;

		// For each hover point...
		for (int hoverPointIndex = 0; hoverPointIndex < hoverPoints.Length; hoverPointIndex++) {

			// Casts a ray on the gravity direction to find the closest point in the track
			RaycastHit trackHit;
			if (Physics.Raycast(hoverPoints[hoverPointIndex].position, _gravity, out trackHit, hoverDistance, trackCollisionLayer)) {

				// Accumulates their values
				hoverNormal += trackHit.SmoothedNormal();
				rayHitCount++;

				// Adds force to the vehicle to separate it from the track
				// The amount of force added is proportional to how close the vehicle is to the ground
				float proportionalDistance = 1f - trackHit.distance / hoverDistance;
				_rigidbody.AddForceAtPosition(trackHit.normal * proportionalDistance * hoverForce / hoverPoints.Length, hoverPoints[hoverPointIndex].position);
			}
		}

		if (rayHitCount == 0) {

			// If none of the ray hits, uses the gravity direction as normal
			hoverNormal = -_gravity;
			Grounded = false;
		}
		else {

			// Calculates the average value
			hoverNormal /= rayHitCount;
			Grounded = true;

			// In order to smooth the movement, projects the velocity into the track's curvature
			Vector3 projectedVelocity = Vector3.ProjectOnPlane(_rigidbody.velocity, hoverNormal);
			_rigidbody.velocity = projectedVelocity.normalized * _rigidbody.velocity.magnitude;
		}

		// Orientates the vehicle to match the normal
		Vector3 projectedForward = Vector3.ProjectOnPlane(_transform.forward, hoverNormal);
		Quaternion targetRotation = Quaternion.LookRotation(projectedForward, hoverNormal);
		_transform.rotation = Quaternion.Lerp(_transform.rotation, targetRotation, gravityReorientationSpeed * Time.deltaTime);
	}

	private void TurnVehicle() {

		// Rotates the forward direction using the turn rate
		float amountTurned = turnRate * _horizontalInput * Time.deltaTime;
		amountTurned *= GetVelocityFactor();
		Quaternion turnRotation = Quaternion.AngleAxis(amountTurned, _transform.up);
		_transform.rotation = turnRotation * _transform.rotation;
		_rigidbody.velocity = turnRotation * _rigidbody.velocity;
	}

	private void TiltModel() {

		// Tilts the model, creating a better sensation of speed
		float weightedTiltSpeed = tiltSpeed;
		weightedTiltSpeed *= GetVelocityFactor();
		Quaternion tiltRotation = Quaternion.AngleAxis(-tiltAngle * _horizontalInput, Vector3.forward);
		model.localRotation = Quaternion.Lerp(model.localRotation, tiltRotation, weightedTiltSpeed);
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

	private float GetVelocityFactor() {
		return 1f - _rigidbody.velocity.magnitude / maxSpeed;
	}
}