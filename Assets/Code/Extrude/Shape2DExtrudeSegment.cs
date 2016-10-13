﻿using UnityEngine;
using System;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class Shape2DExtrudeSegment : MonoBehaviour {

	public enum ControlPointRotation {
		Manual,
		AutomaticNormals,
		AutomaticOrientation,
		AutomaticBoth
	}

	public enum InterpolationMethod {
		Linear,
		Bezier
	}

	public Shape2D shape;
	public int resolution = 5;
	public InterpolationMethod interpolationMethod = InterpolationMethod.Bezier;
	public bool recalculateNormals = true;
	public ControlPointRotation controlPointRotation = ControlPointRotation.Manual;

	private Shape2DExtrudeControlPoint[] _controlPoints;

	private MeshFilter _meshFilter;

	void Awake() {
		// Retrieves the desired components
		_meshFilter = GetComponent<MeshFilter>();

		// Finds all the control points assigned to this segment
		_controlPoints = this.GetComponentsInChildrenOnly<Shape2DExtrudeControlPoint>();
	}
	
	void Start() {
		if (shape == null) {
			Debug.LogWarning("WARNING: No shape selected!");
			return;
		}

		// Calculates the rotation of the control points
		CalculateControlPointsRotation();

		// Extrudes the shape using the control points
		ExtrudeShape();
	}

	void Update() {
		if (!Application.isPlaying) {
			Awake();
			Start();
		}
	}

	public Shape2DExtrudeControlPoint[] GetControlPoints() {
		return _controlPoints;
	}

	private void CalculateControlPointsRotation() {

		// At least two points are needed to calculae their rotation
		if (_controlPoints.Length < 2)
			return;

		// For each control point...
		for (int controlPointIndex = 1; controlPointIndex < _controlPoints.Length - 1; controlPointIndex++) {

			// Calculates the direction from the previous control point
			Vector3 directionFromPrevious = _controlPoints[controlPointIndex].GetPosition() - _controlPoints[controlPointIndex - 1].GetPosition();

			// Calculates the direction to the next control point
			Vector3 directionToNext = _controlPoints[controlPointIndex + 1].GetPosition() - _controlPoints[controlPointIndex].GetPosition();

			// Calculates the related directions based on the previous ones
			Vector3 upDirection = _controlPoints[controlPointIndex].GetUpDirection();
			Vector3 forwardDirection = _controlPoints[controlPointIndex].GetForwardDirection();
			Vector3 tangentDirection = directionFromPrevious + directionToNext;

			// The normal needs more calculations
			Vector3 proxUpDirection = _controlPoints[controlPointIndex - 1].GetUpDirection() + _controlPoints[controlPointIndex + 1].GetUpDirection();
			Vector3 normalDirection = Vector3.Cross(directionFromPrevious, directionToNext);
			if (Vector3.Dot(proxUpDirection, normalDirection) < 0)
				normalDirection *= -1;

			// Calculates the rotation based on the segment's configuration
			Quaternion rotation;
			switch (controlPointRotation) {
				case ControlPointRotation.Manual:

					// Keep the control point's rotation
					rotation = _controlPoints[controlPointIndex].GetRotation();
					break;
				case ControlPointRotation.AutomaticNormals:

					// Keep the control point's orientation, but modify the normal
					rotation = Quaternion.LookRotation(forwardDirection, normalDirection);
					break;
				case ControlPointRotation.AutomaticOrientation:
					
					// Keep the control point's normal, but modify the orientation
					rotation = Quaternion.LookRotation(tangentDirection, upDirection);
					break;
				case ControlPointRotation.AutomaticBoth:

					// Modify both the control point's normal and orientation
					rotation = Quaternion.LookRotation(tangentDirection, normalDirection);
					break;
				default:
					throw new InvalidOperationException("The selected rotation method is not supported: " + controlPointRotation);
			}

			// Assigns the new rotation
			_controlPoints[controlPointIndex].GetTransform().localRotation = rotation;
		}
	}

	private void ExtrudeShape() {
		// At least two control points are needed to extrude the shape
		if (_controlPoints.Length < 2) {
			Debug.LogWarning("WARNING: At least 2 control points needed to extrude!");
			return;
		}

		// Creates and populates the mesh
		Mesh mesh = new Mesh();
		mesh.vertices = CreateVertices();
		mesh.uv = CreateUVs();
		mesh.triangles = CreateTriangles();

		// Calculates the mesh's normals
		if (recalculateNormals)
			mesh.RecalculateNormals();
		else
			mesh.normals = CreateNormals();

		// Recalculates the mesh's bounds
		mesh.RecalculateBounds();

		// Assigns the mesh in the mesh filter
		_meshFilter.sharedMesh = mesh;
	}

	private Vector3[] CreateVertices() {
		// Creates the vertices
		Vector3[] meshVertices = new Vector3[resolution * _controlPoints.Length * shape.points.Length];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {

			// For each resolution pass...
			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {

				// Calculates the interpolated values
				float lerpFactor = controlPointIndex + (float)resolutionPass / resolution;
				Vector3 interpolatedPosition = InterpolatePosition(lerpFactor);
				Quaternion interpolatedRotation = InterpolateRotation(lerpFactor);
				Vector3 interpolatedScale = InterpolateScale(lerpFactor);

				// Caches some values
				int meshVertexBaseIndex = (controlPointIndex * resolution + resolutionPass) * shape.points.Length;

				// For each point in the shape...
				for (int shapePointIndex = 0; shapePointIndex < shape.points.Length; shapePointIndex++) {

					// Creates a vertex for each point using the interpolated information
					int meshVertexIndex = meshVertexBaseIndex + shapePointIndex;
					meshVertices[meshVertexIndex] = Shape2DExtrudeControlPoint.TransformPoint(shape.points[shapePointIndex], interpolatedPosition, interpolatedRotation, interpolatedScale);
				}

				// The last control point only has one resolution pass!
				if (controlPointIndex == _controlPoints.Length - 1)
					break;
			}
		}

		return meshVertices;
	}

	private Vector3[] CreateNormals() {
		// Creates the normals
		Vector3[] meshNormals = new Vector3[resolution * _controlPoints.Length * shape.normals.Length];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {

			// For each resolution pass...
			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {

				// Calculates the interpolated values
				float lerpFactor = controlPointIndex + (float)resolutionPass / resolution;
				Quaternion interpolatedRotation = InterpolateRotation(lerpFactor);

				// Caches some values
				int meshNormalBaseIndex = (controlPointIndex * resolution + resolutionPass) * shape.normals.Length;

				// For each normal in the shape...
				for (int shapeNormalIndex = 0; shapeNormalIndex < shape.normals.Length; shapeNormalIndex++) {

					// Assigns the normal to each vertex using the interpolated information
					int meshNormalIndex = meshNormalBaseIndex + shapeNormalIndex;
					meshNormals[meshNormalIndex] = Shape2DExtrudeControlPoint.TransformDirection(shape.normals[shapeNormalIndex], interpolatedRotation);
				}

				// The last control point only has one resolution pass!
				if (controlPointIndex == _controlPoints.Length - 1)
					break;
			}
		}

		return meshNormals;
	}

	private Vector2[] CreateUVs() {
		// Creates the UVs
		Vector2[] meshUVs = new Vector2[resolution * _controlPoints.Length * shape.us.Length];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {

			// For each resolution pass...
			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {

				// Calculates the interpolated values
				float lerpFactor = controlPointIndex + (float)resolutionPass / resolution;
				float interpolatedV = lerpFactor / (_controlPoints.Length - 1);

				// Caches some values
				int meshUVBaseIndex = (controlPointIndex * resolution + resolutionPass) * shape.us.Length;

				// For each U in the shape...
				for (int shapeUIndex = 0; shapeUIndex < shape.us.Length; shapeUIndex++) {

					// Assigns the UVs to each vertex using the interpolated information
					int meshUVIndex = meshUVBaseIndex + shapeUIndex;
					meshUVs[meshUVIndex] = new Vector2(shape.us[shapeUIndex], interpolatedV);
				}
			}

			// The last control point only has one resolution pass!
			if (controlPointIndex == _controlPoints.Length - 1)
				break;
		}

		return meshUVs;
	}

	private int[] CreateTriangles() {
		// Creates the vertices
		int trianglesCount = 3 * resolution * (_controlPoints.Length - 1) * shape.lines.Length;
		int[] meshTriangles = new int[trianglesCount];

		// For each control point...
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length - 1; controlPointIndex++) {

			// For each resolution pass...
			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {

				// Caches some values
				int meshTriangleBaseIndex = 3 * (resolutionPass + controlPointIndex * resolution ) * shape.lines.Length;
				int meshVertexBaseIndex =  (resolutionPass + controlPointIndex * resolution) * shape.lines.Length;
				int meshVertexNextBaseIndex = (resolutionPass + 1 + controlPointIndex * resolution) * shape.lines.Length;

				// For each line in the shape...
				for (int shapeLineIndex = 0; shapeLineIndex < shape.lines.Length; shapeLineIndex += 2) {

					// Each line generates 2 triangles, then 6 vertices
					int currentTriangle = meshTriangleBaseIndex + 3 * shapeLineIndex;

					// Creates the first triangle
					meshTriangles[currentTriangle] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex];
					meshTriangles[currentTriangle + 1] = meshVertexBaseIndex + shape.lines[shapeLineIndex + 1];
					meshTriangles[currentTriangle + 2] = meshVertexBaseIndex + shape.lines[shapeLineIndex];

					// Creates the first triangle
					meshTriangles[currentTriangle + 3] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex + 1];
					meshTriangles[currentTriangle + 4] = meshVertexBaseIndex + shape.lines[shapeLineIndex + 1];
					meshTriangles[currentTriangle + 5] = meshVertexNextBaseIndex + shape.lines[shapeLineIndex];
				}
			}
		}

		return meshTriangles;
	}

	public Vector3 InterpolatePosition(float interpolationFactor) {
		Shape2DExtrudeControlPoint startControlPoint = _controlPoints[Mathf.FloorToInt(interpolationFactor)];
		Shape2DExtrudeControlPoint endControlPoint = _controlPoints[Mathf.CeilToInt(interpolationFactor)];
		float lerpFactor = interpolationFactor - Mathf.Floor(interpolationFactor);
		switch (interpolationMethod) {
			case InterpolationMethod.Linear:
				return Vector3.Lerp(startControlPoint.GetPosition(), endControlPoint.GetPosition(), lerpFactor);
			case InterpolationMethod.Bezier:
				return BezierCurve.BezierPosition(startControlPoint.GetPosition(), startControlPoint.GetForwardHandlePosition(), endControlPoint.GetPosition(), endControlPoint.GetBackwardHandlePosition(), lerpFactor);
			default:
				throw new InvalidOperationException("The current interpolation method is not supported: " + interpolationMethod);
		}
	}

	public Quaternion InterpolateRotation(float interpolationFactor) {
		Shape2DExtrudeControlPoint startControlPoint = _controlPoints[Mathf.FloorToInt(interpolationFactor)];
		Shape2DExtrudeControlPoint endControlPoint = _controlPoints[Mathf.CeilToInt(interpolationFactor)];
		float lerpFactor = interpolationFactor - Mathf.Floor(interpolationFactor);
		switch (interpolationMethod) {
			case InterpolationMethod.Linear:
				return Quaternion.Lerp(startControlPoint.GetRotation(), endControlPoint.GetRotation(), lerpFactor);
			case InterpolationMethod.Bezier:
				Vector3 upDirection = Vector3.Lerp(startControlPoint.GetUpDirection(), endControlPoint.GetUpDirection(), lerpFactor);
				return BezierCurve.BezierOrientation(startControlPoint.GetPosition(), startControlPoint.GetForwardHandlePosition(), endControlPoint.GetPosition(), endControlPoint.GetBackwardHandlePosition(), lerpFactor, upDirection);
			default:
				throw new InvalidOperationException("The current interpolation method is not supported: " + interpolationMethod);
		}
	}

	public Vector3 InterpolateScale(float interpolationFactor) {
		Shape2DExtrudeControlPoint startControlPoint = _controlPoints[Mathf.FloorToInt(interpolationFactor)];
		Shape2DExtrudeControlPoint endControlPoint = _controlPoints[Mathf.CeilToInt(interpolationFactor)];
		float lerpFactor = interpolationFactor - Mathf.Floor(interpolationFactor);
		return Vector3.Lerp(startControlPoint.GetScale(), endControlPoint.GetScale(), lerpFactor);
	}

	void OnDrawGizmosSelected() {
		Vector3 previousPosition = _controlPoints[0].GetPosition();
		Matrix4x4 originalMatrix = Gizmos.matrix;
		Gizmos.matrix = transform.localToWorldMatrix;
		for (int controlPointIndex = 0; controlPointIndex < _controlPoints.Length; controlPointIndex++) {

			for (int resolutionPass = 0; resolutionPass < resolution; resolutionPass++) {
				if (controlPointIndex == 0 && resolutionPass == 0)
					continue;

				float factor = controlPointIndex + (float)resolutionPass / resolution;
				Vector3 position = InterpolatePosition(factor);
				Gizmos.color = Color.green;
				Gizmos.DrawSphere(position, Shape2DExtrudeControlPoint.gizmosRadius / 4);

				Quaternion rotation = InterpolateRotation(factor);
				Gizmos.color = Color.blue;
				Gizmos.DrawRay(position, rotation * Vector3.up);

				Gizmos.color = Color.green;
				Gizmos.DrawLine(previousPosition, position);
				previousPosition = position;

				if (controlPointIndex == _controlPoints.Length - 1)
					break;
			}
		}
		Gizmos.matrix = originalMatrix;
	}
}