using UnityEngine;

public class VehicleCamera : MonoBehaviour {

	public Vector3 cameraOffset = new Vector3(0, 1.5f, 3.5f);
	public float forwardDisplacement = 5f;
	public float cameraStickiness = 10f;
	public float cameraRotationSpeed = 5f;
	public float rampMagnitude = 1f;
	public float rampRotationSpeed = 25f;
	public float tiltFactor = 0.5f;
	public float tiltRotationSpeed = 5f;
	public float minFOV = 90f;
	public float maxFOV = 120f;

	private Transform _vehicleTransform;
	private VehicleController _vehicleController;
	private Transform _cameraTransfrom;
	private Transform _transform;

	private Vector3 _targetPosition;
	private float _rampRotation;
	private float _tiltRotation;

	void Awake() {

		// Retrieves the desired components
		_transform = transform;
		_vehicleTransform = _transform.parent.GetComponent<Transform>();
		_vehicleController = _vehicleTransform.GetComponent<VehicleController>();
		_cameraTransfrom = GetComponentInChildren<Camera>().transform;
	}

	void Start() {

		// Detach the camera so that it can move freely on its own
		_transform.parent = null;
	}

	void FixedUpdate() {

		// Moves the camera to match the vehicles's position
		_targetPosition = Vector3.Lerp(_targetPosition, _vehicleTransform.position, cameraStickiness * Time.deltaTime);
		_transform.position = _targetPosition;

		// Orientates the camera to the vehicles's forward direction
		Quaternion look = Quaternion.LookRotation(_vehicleTransform.forward, _vehicleTransform.up);
		_transform.rotation = Quaternion.Slerp(_transform.rotation, look, cameraRotationSpeed * Time.deltaTime);

		// Rotates the camera arround the vehicle on ramps
		_rampRotation = Mathf.MoveTowards(_rampRotation, _vehicleController.RampFactor * -rampMagnitude, rampRotationSpeed * Time.deltaTime);
		_cameraTransfrom.localPosition = Quaternion.AngleAxis(_rampRotation, Vector3.right) * cameraOffset;
		_cameraTransfrom.LookAt(_vehicleTransform.position + _vehicleTransform.forward * forwardDisplacement, _vehicleTransform.up);

		// Rotates the camera using the vehicles tilt
		float targetTiltRotation = _vehicleController.CurrentTilt * tiltFactor;
		_tiltRotation = Mathf.Lerp(_tiltRotation, targetTiltRotation, tiltRotationSpeed * Time.deltaTime);
		_cameraTransfrom.rotation = Quaternion.AngleAxis(_tiltRotation, _cameraTransfrom.forward) * _cameraTransfrom.rotation;

		// Adjusts the camera FOV
		float velocityFactor = _vehicleController.GetVelocityFactor();
		_cameraTransfrom.GetComponent<Camera>().fieldOfView = Mathf.Lerp(minFOV, maxFOV, velocityFactor);
	}
}