using UnityEngine;

public class Shape2DExtrudeControlPoint : MonoBehaviour {

	public float gizmosRadius = 0.25f;

	private Transform _transform;

	void Awake() {
		_transform = transform;
	}

	public Vector3 GetPosition() {
		return _transform.localPosition;
	}

	public Vector3 TransformPoint(Vector3 point) {
		return TransformPoint(point, _transform.localPosition, _transform.localRotation, _transform.localScale);
	}

	public static Vector3 TransformPoint(Vector3 point, Vector3 position, Quaternion rotation, Vector3 scale) {
		return position + rotation * Vector3.Scale(point, scale);
	}

	void OnDrawGizmos() {
		Gizmos.DrawSphere(transform.position, gizmosRadius);
	}
}