using UnityEngine;

[ExecuteInEditMode]
public class Shape2DExtrudeControlPoint : MonoBehaviour {

	public float gizmosRadius = 0.25f;

	private Transform _transform;

	void Awake() {
		_transform = transform;
	}

	public Vector3 GetPosition() {
		return _transform.localPosition;
	}

	public Quaternion GetRotation() {
		return _transform.localRotation;
	}

	public Vector3 GetScale() {
		return _transform.localScale;
	}

	public Vector3 TransformPoint(Vector3 point) {
		return TransformPoint(point, _transform.localPosition, _transform.localRotation, _transform.localScale);
	}

	public Vector3 TransformDirection(Vector3 direction) {
		return TransformDirection(direction, _transform.localRotation);
	}

	public static Vector3 TransformPoint(Vector3 point, Vector3 position, Quaternion rotation, Vector3 scale) {
		return position + rotation * Vector3.Scale(point, scale);
	}

	public static Vector3 TransformDirection(Vector3 direction, Quaternion rotation) {
		return rotation * direction;
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere(transform.position, gizmosRadius);
	}
}