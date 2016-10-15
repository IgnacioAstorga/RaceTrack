using UnityEngine;
using System;

[ExecuteInEditMode]
public class Shape2DExtrudeControlPoint : MonoBehaviour {

	public static readonly float gizmosRadius = 0.5f;
	
	private Transform _transform;
	private Shape2DExtrudeSegment _segment;

	void Awake() {
		_transform = transform;
		_segment = GetComponentInParent<Shape2DExtrudeSegment>();

		if (_segment == null)
			Debug.LogWarning("The control point is not child of a Shape2DExtrudeSegment!");
	}

	public Transform GetTransform() {
		return _transform;
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

	public Vector3 GetForwardHandlePosition() {
		return GetPosition() + GetForwardDirection() * GetScale().z;
	}

	public Vector3 GetBackwardHandlePosition() {
		return GetPosition() - GetForwardDirection() * GetScale().z;
	}

	public Vector3 GetForwardDirection() {
		return _transform.localRotation * Vector3.forward;
	}

	public Vector3 GetUpDirection() {
		return _transform.localRotation * Vector3.up;
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
		if (!Application.isPlaying)
			Awake();

		if (_segment == null)
			return;

		Gizmos.color = Color.cyan;
		Vector3 position = _transform.position;
		float radius = _transform.lossyScale.x * gizmosRadius;
		Gizmos.DrawSphere(position, radius);

		if (_segment.interpolationMethod == Shape2DExtrudeSegment.InterpolationMethod.Bezier) {
			Gizmos.color = Color.yellow;
			int controlPointIndex = Array.IndexOf(_segment.GetControlPoints(), this);
			if (controlPointIndex != 0) {
				Vector3 backwardHandle = _transform.position - _transform.forward * _transform.lossyScale.z;
				Gizmos.DrawLine(position, backwardHandle);
				Gizmos.DrawSphere(backwardHandle, radius / 2);
			}
			if (controlPointIndex != _segment.GetControlPoints().Length - 1) {
				Vector3 forwardHandle = _transform.position + _transform.forward * _transform.lossyScale.z;
				Gizmos.DrawLine(position, forwardHandle);
				Gizmos.DrawSphere(forwardHandle, radius / 2);
			}
		}
	}
}