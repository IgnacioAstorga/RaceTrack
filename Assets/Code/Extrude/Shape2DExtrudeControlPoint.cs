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

	void OnDrawGizmos() {
		Gizmos.DrawSphere(transform.position, gizmosRadius);
	}
}