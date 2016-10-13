using UnityEngine;

public static class BezierCurve {

	public static Vector3 BezierPosition(Vector3 start, Vector3 startHandle, Vector3 end, Vector3 endHandle, float factor) {
		float inverseFactor = 1 - factor;
		float inverseFactorSquared = inverseFactor * inverseFactor;
		float factorSquared = factor * factor;
		return
			start * (inverseFactorSquared * inverseFactor) +
			startHandle * (3 * inverseFactorSquared * factor) +
			endHandle * (3 * inverseFactor * factorSquared) +
			end * (factorSquared * factor);
	}

	public static Vector3 BezierTangent(Vector3 start, Vector3 startHandle, Vector3 end, Vector3 endHandle, float factor) {
		float inverseFactor = 1 - factor;
		float inverseFactorSquared = inverseFactor * inverseFactor;
		float factorSquared = factor * factor;
		Vector3 tangent =
			start * (-inverseFactorSquared) +
			startHandle * (3 * inverseFactorSquared - 2 * inverseFactor) +
			endHandle * (-3 * factorSquared + 2 * factor) +
			end * (factorSquared);
		return tangent.normalized;
	}

	public static Vector2 BezierNormal2D(Vector3 start, Vector3 startHandle, Vector3 end, Vector3 endHandle, float factor) {
		Vector3 tangent = BezierTangent(start, startHandle, end, endHandle, factor);
		return new Vector2(-tangent.y, tangent.x);
	}

	public static Vector2 BezierNormal(Vector3 start, Vector3 startHandle, Vector3 end, Vector3 endHandle, float factor, Vector3 up) {
		Vector3 tangent = BezierTangent(start, startHandle, end, endHandle, factor);
		Vector3 binormal = Vector3.Cross(up, tangent).normalized;
		return Vector3.Cross(tangent, binormal);
	}

	public static Quaternion BezierOrientation2D(Vector3 start, Vector3 startHandle, Vector3 end, Vector3 endHandle, float factor) {
		Vector3 tangent = BezierTangent(start, startHandle, end, endHandle, factor);
		Vector3 normal = BezierNormal2D(start, startHandle, end, endHandle, factor);
		return Quaternion.LookRotation(tangent, normal);
	}

	public static Quaternion BezierOrientation(Vector3 start, Vector3 startHandle, Vector3 end, Vector3 endHandle, float factor, Vector3 up) {
		Vector3 tangent = BezierTangent(start, startHandle, end, endHandle, factor);
		Vector3 normal = BezierNormal(start, startHandle, end, endHandle, factor, up);
		return Quaternion.LookRotation(tangent, normal);
	}
}